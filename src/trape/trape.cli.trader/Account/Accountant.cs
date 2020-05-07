using Binance.Net.Interfaces;
using Binance.Net.Objects;
using Binance.Net.Objects.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache;
using trape.datalayer;
using trape.jobs;
using trape.mapper;

namespace trape.cli.trader.Account
{
    /// <summary>
    /// Implementation of <c>IAccountant</c> managing the binance inventory
    /// </summary>
    public class Accountant : IAccountant
    {
        #region Fields

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Account Info synchronizer
        /// </summary>
        private readonly Job _jobSynchronizeAccountInfo;

        /// <summary>
        /// Binance connection keepalive
        /// </summary>
        private readonly Job _jobConnectionKeepAlive;

        /// <summary>
        /// Binance Client
        /// </summary>
        private readonly IBinanceClient _binanceClient;

        /// <summary>
        /// Binance Socket Client
        /// </summary>
        private readonly IBinanceSocketClient _binanceSocketClient;

        /// <summary>
        /// Account Info
        /// </summary>
        private BinanceAccountInfo _binanceAccountInfo;

        /// <summary>
        /// Last time the binance account info was updated
        /// </summary>
        private DateTime _binanceAccountInfoUpdated;

        /// <summary>
        /// Stream account info
        /// </summary>
        private BinanceStreamAccountInfo _binanceStreamAccountInfo;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Buffer
        /// </summary>
        private readonly IBuffer _buffer;

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Binance listen key
        /// </summary>
        private string _binanceListenKey;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Accountant</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="binanceClient">Binance Client</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public Accountant(ILogger logger, IBuffer buffer, IBinanceClient binanceClient, IBinanceSocketClient binanceSocketClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _ = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            _ = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            #endregion

            this._logger = logger.ForContext<Accountant>();
            this._buffer = buffer;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._binanceClient = binanceClient;
            this._binanceSocketClient = binanceSocketClient;
            this._binanceAccountInfoUpdated = default;

            #region Job Setup

            // Create timer for account info synchronization
            this._jobSynchronizeAccountInfo = new Job(new TimeSpan(0, 0, 5), _synchronizeAccountInfo);

            // Create timer for connection keep alive
            this._jobConnectionKeepAlive = new Job(new TimeSpan(0, 15, 0), _connectionKeepAlive);

            #endregion
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Sends ping to binance to keep the connection alive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _connectionKeepAlive()
        {
            // Test connection to binance
            var ping = await this._binanceClient.PingAsync(this._cancellationTokenSource.Token).ConfigureAwait(false);

            if (ping.Success)
            {
                this._logger.Verbose("Ping successful");

                // Send keep alive packages
                var userStreamKeepAlive = await this._binanceClient.KeepAliveUserStreamAsync(this._binanceListenKey, ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
                if (userStreamKeepAlive.Success)
                {
                    this._logger.Verbose("User Stream Keep Alive successful");
                }
                else
                {
                    this._logger.Verbose("User Stream Keep Alive NOT successful");
                }
            }
            else
            {
                this._logger.Verbose("Ping NOT successful");
            }
        }

        /// <summary>
        /// Synchronizes the account info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _synchronizeAccountInfo()
        {
            // Get latest account information
            var accountInfo = await this._binanceClient.GetAccountInfoAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
            if (accountInfo.Success)
            {
                this._binanceAccountInfo = accountInfo.Data;
                this._binanceAccountInfoUpdated = DateTime.UtcNow;
            }

            this._logger.Verbose("Account info synchronized");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Receives Binance account info
        /// </summary>
        /// <param name="binanceStreamAccountInfo"></param>
        private void _saveBinanceStreamAccountInfo(BinanceStreamAccountInfo binanceStreamAccountInfo)
        {
            this._binanceStreamAccountInfo = binanceStreamAccountInfo;
            this._logger.Verbose("Received Binance Stream Account Info");
        }

        /// <summary>
        /// Receives Binance order updates
        /// </summary>
        /// <param name="binanceStreamOrderUpdate"></param>
        private async void _saveBinanceStreamOrderUpdate(BinanceStreamOrderUpdate binanceStreamOrderUpdate)
        {
            var database = new TrapeContext(Program.Services.GetService<DbContextOptions<TrapeContext>>(), Program.Services.GetService<ILogger>());
            try
            {
                // Save Order Update
                database.OrderUpdates.Add(Translator.Translate(binanceStreamOrderUpdate));

                // If sell and filled, remove quantity from buy
                if (binanceStreamOrderUpdate.Side == OrderSide.Sell && binanceStreamOrderUpdate.Status == OrderStatus.Filled)
                {
                    // Get trades where assets were bought
                    var buyTrades = database.PlacedOrders
                                    .Where(p => p.Side == datalayer.Enums.OrderSide.Buy
                                        && p.Symbol == binanceStreamOrderUpdate.Symbol)
                                    .SelectMany(f => f.Fills.Where(f => f.Quantity > f.ConsumedQuantity
                                                                && f.Price <= binanceStreamOrderUpdate.Price))
                                    .OrderByDescending(t => t.Price);

                    var soldQuantity = binanceStreamOrderUpdate.AccumulatedQuantityOfFilledTrades;

                    // Fill trades with sold quantity
                    foreach(var trade in buyTrades)
                    {
                        var freeQuantity = trade.Quantity - trade.ConsumedQuantity;

                        // Free quantity is more than soldQuantity, substract all from one trade
                        if(freeQuantity > soldQuantity)
                        {
                            trade.ConsumedQuantity -= soldQuantity;
                            soldQuantity = 0;
                            break;
                        }
                        else
                        {
                            // Substract what is available
                            // Substract what can be used for the trade
                            soldQuantity -= (trade.Quantity - trade.ConsumedQuantity);
                            trade.ConsumedQuantity = trade.Quantity;                            
                        }

                        if(soldQuantity == 0)
                        {
                            break;
                        }
                    }

                    // In case it was sold over price, remove from smallest over price first
                    if(soldQuantity != 0)
                    {
                        // Get trades where assets were bought
                        var overPriceBuyTrades = database.PlacedOrders
                                        .Where(p => p.Side == datalayer.Enums.OrderSide.Buy
                                            && p.Symbol == binanceStreamOrderUpdate.Symbol)
                                        .SelectMany(f => f.Fills.Where(f => f.Quantity > f.ConsumedQuantity
                                                                    && f.Price > binanceStreamOrderUpdate.Price))
                                        .OrderBy(t => t.Price);
                        
                        // Fill trades with sold quantity
                        foreach (var trade in overPriceBuyTrades)
                        {
                            var freeQuantity = trade.Quantity - trade.ConsumedQuantity;

                            // Free quantity is more than soldQuantity, substract all from one trade
                            if (freeQuantity > soldQuantity)
                            {
                                trade.ConsumedQuantity -= soldQuantity;
                                soldQuantity = 0;
                                break;
                            }
                            else
                            {
                                // Substract what is available
                                // Substract what can be used for the trade
                                soldQuantity -= (trade.Quantity - trade.ConsumedQuantity);
                                trade.ConsumedQuantity = trade.Quantity;
                            }

                            if (soldQuantity == 0)
                            {
                                break;
                            }
                        }
                    }
                }

                await database.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logger = Program.Services.GetService(typeof(ILogger)) as ILogger;
                logger.ForContext(typeof(Accountant));
                logger.Error(e.Message, e);
            }

            // Clear blocked amount
            if (binanceStreamOrderUpdate.Status == OrderStatus.Filled
                || binanceStreamOrderUpdate.Status == OrderStatus.Rejected
                || binanceStreamOrderUpdate.Status == OrderStatus.Expired
                || binanceStreamOrderUpdate.Status == OrderStatus.Canceled)
            {
                this._buffer.RemoveOpenOrder(binanceStreamOrderUpdate.ClientOrderId);
            }

            this._logger.Verbose("Received Binance Stream Order Update");
        }

        /// <summary>
        /// Receives Binance order lists
        /// </summary>
        /// <param name="binanceStreamOrderList"></param>
        private async void _saveBinanceStreamOrderList(BinanceStreamOrderList binanceStreamOrderList)
        {
            var database = new TrapeContext(Program.Services.GetService<DbContextOptions<TrapeContext>>(), Program.Services.GetService<ILogger>());
            try
            {
                database.OrderLists.Add(Translator.Translate(binanceStreamOrderList));
                await database.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logger = Program.Services.GetService(typeof(ILogger)) as ILogger;
                logger.ForContext(typeof(Accountant));
                logger.Error(e.Message, e);
            }

            this._logger.Verbose("Received Binance Stream Order List");
        }

        /// <summary>
        /// Receives Binance balances
        /// </summary>
        /// <param name="binanceStreamBalances"></param>
        private async void _saveBinanceStreamBalance(IEnumerable<BinanceStreamBalance> binanceStreamBalances)
        {
            var database = new TrapeContext(Program.Services.GetService<DbContextOptions<TrapeContext>>(), Program.Services.GetService<ILogger>());
            try
            {
                database.Balances.AddRange(Translator.Translate(binanceStreamBalances));
                await database.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logger = Program.Services.GetService(typeof(ILogger)) as ILogger;
                logger.ForContext(typeof(Accountant));
                logger.Error(e.Message, e);
            }

            this._logger.Verbose("Received Binance Stream Balances");
        }

        /// <summary>
        /// Receives Binance balance updates
        /// </summary>
        /// <param name="binanceStreamBalanceUpdate"></param>
        private async void _saveBinanceStreamBalanceUpdate(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate)
        {
            var database = new TrapeContext(Program.Services.GetService<DbContextOptions<TrapeContext>>(), Program.Services.GetService<ILogger>());
            try
            {
                database.BalanceUpdates.AddRange(Translator.Translate(binanceStreamBalanceUpdate));
                await database.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logger = Program.Services.GetService(typeof(ILogger)) as ILogger;
                logger.ForContext(typeof(Accountant));
                logger.Error(e.Message, e);
            }

            this._logger.Verbose("Received Binance Stream Balance Update");
        }

        /// <summary>
        /// Returns the balance of an asset
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>Returns the balance or null if no balance available for the asset</returns>
        public async Task<BinanceBalance> GetBalance(string asset)
        {
            // Take reference to original instance in case _binanceAccountInfo is updated
            var bac = await this.GetAccountInfo().ConfigureAwait(true);

            return bac?.Balances.SingleOrDefault(b => b.Asset == asset);
        }

        /// <summary>
        /// Returns the latest account information
        /// </summary>
        /// <returns></returns>
        public async Task<BinanceAccountInfo> GetAccountInfo()
        {
            var bac = this._binanceAccountInfo;

            if (bac == null || this._binanceAccountInfoUpdated < DateTime.UtcNow.AddSeconds(-6))
            {
                var accountInfoRequest = await this._binanceClient.GetAccountInfoAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(true);

                if (accountInfoRequest.Success)
                {
                    this._binanceAccountInfo = accountInfoRequest.Data;
                    this._binanceAccountInfoUpdated = DateTime.UtcNow;

                    this._logger.Debug($"Requested account info");
                }
                else
                {
                    // Something is oddly wrong, wait a bit
                    this._logger.Debug("Cannot retrieve account info");
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }

            return this._binanceAccountInfo;
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Starts the <c>Accountant</c>
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task Start()
        {
            this._logger.Verbose("Starting Accountant");

            // Connect to user stream
            var result = await this._binanceClient.StartUserStreamAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(true);
            if (result.Success)
            {
                this._binanceListenKey = result.Data;
                this._logger.Verbose("Connection ok, ListenKey received");
            }

            // Run initial keep alive
            this._connectionKeepAlive();

            // Subscribe to socket events
            await this._binanceSocketClient.SubscribeToUserDataUpdatesAsync(this._binanceListenKey,
                (bsai) => _saveBinanceStreamAccountInfo(bsai),
                (bsou) => _saveBinanceStreamOrderUpdate(bsou),
                (bsol) => _saveBinanceStreamOrderList(bsol),
                (bsbs) => _saveBinanceStreamBalance(bsbs),
                (bsbu) => _saveBinanceStreamBalanceUpdate(bsbu)
                ).ConfigureAwait(true);

            this._logger.Information("Binance Client is online");

            // Start jobs
            this._jobSynchronizeAccountInfo.Start();
            this._jobConnectionKeepAlive.Start();

            this._logger.Debug("Accountant started");
        }

        /// <summary>
        /// Stops the <c>Accountant</c>
        /// </summary>
        /// <returns></returns>
        public async Task Finish()
        {
            this._logger.Verbose("Stopping accountant");

            // Stop jobs
            this._jobSynchronizeAccountInfo.Terminate();
            this._jobConnectionKeepAlive.Terminate();

            // Wait until timer elapsed event finishes
            await Task.Delay(1000).ConfigureAwait(true);

            this._cancellationTokenSource.Cancel();

            this._logger.Debug("Accountant stopped");
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._jobSynchronizeAccountInfo.Dispose();
                this._binanceClient.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
