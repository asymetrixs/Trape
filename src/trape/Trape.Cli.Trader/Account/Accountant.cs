using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.SpotData;
using Binance.Net.Objects.Spot.UserStream;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.trader.Listener;
using Trape.Datalayer;
using Trape.Datalayer.Models;
using Trape.Jobs;
using Trape.Mapper;

namespace Trape.Cli.trader.Account
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
        private readonly IListener _buffer;

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Binance listen key
        /// </summary>
        private string _binanceListenKey;

        /// <summary>
        /// Synchronize stream order updates
        /// </summary>
        private readonly SemaphoreSlim _syncBinanceStreamOrderUpdate;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Accountant</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="binanceClient">Binance Client</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public Accountant(ILogger logger, IListener buffer, IBinanceClient binanceClient, IBinanceSocketClient binanceSocketClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            _binanceSocketClient = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            #endregion

            _logger = logger.ForContext<Accountant>();
            _cancellationTokenSource = new CancellationTokenSource();
            _binanceAccountInfoUpdated = default;
            _syncBinanceStreamOrderUpdate = new SemaphoreSlim(1, 1);

            #region Job Setup

            // Create timer for account info synchronization
            _jobSynchronizeAccountInfo = new Job(new TimeSpan(0, 0, 5), SynchronizeAccountInfo);

            // Create timer for connection keep alive
            _jobConnectionKeepAlive = new Job(new TimeSpan(0, 15, 0), ConnectionKeepAlive);

            #endregion
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Sends ping to binance to keep the connection alive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ConnectionKeepAlive()
        {
            // Test connection to binance
            var ping = await _binanceClient.PingAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

            if (ping.Success)
            {
                _logger.Verbose("Ping successful");

                // Send keep alive packages
                var userStreamKeepAlive = await _binanceClient.Spot.UserStream.KeepAliveUserStreamAsync(_binanceListenKey, ct: _cancellationTokenSource.Token).ConfigureAwait(false);
                if (userStreamKeepAlive.Success)
                {
                    _logger.Verbose("User Stream Keep Alive successful");
                }
                else
                {
                    _logger.Verbose("User Stream Keep Alive NOT successful");
                }
            }
            else
            {
                _logger.Verbose("Ping NOT successful");
            }
        }

        /// <summary>
        /// Synchronizes the account info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SynchronizeAccountInfo()
        {
            // Get latest account information
            var accountInfo = await _binanceClient.General.GetAccountInfoAsync(ct: _cancellationTokenSource.Token).ConfigureAwait(false);
            if (accountInfo.Success)
            {
                _binanceAccountInfo = accountInfo.Data;
                _binanceAccountInfoUpdated = DateTime.UtcNow;
            }

            _logger.Verbose("Account info synchronized");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Receives Binance account info
        /// </summary>
        /// <param name="binanceStreamAccountInfo"></param>
        private void SaveBinanceStreamAccountInfo(BinanceStreamAccountInfo binanceStreamAccountInfo)
        {
            _binanceStreamAccountInfo = binanceStreamAccountInfo;
            _logger.Verbose("Received Binance Stream Account Info");
        }

        /// <summary>
        /// Receives Binance order updates
        /// </summary>
        /// <param name="binanceStreamOrderUpdate"></param>
        private async void SaveBinanceStreamOrderUpdate(BinanceStreamOrderUpdate binanceStreamOrderUpdate)
        {
            try
            {
                // Synchronize access
                await _syncBinanceStreamOrderUpdate.WaitAsync().ConfigureAwait(false);

                using (AsyncScopedLifestyle.BeginScope(Program.Container))
                {
                    var database = Program.Container.GetService<TrapeContext>();

                    // Parse the order update
                    var orderUpdate = Translator.Translate(binanceStreamOrderUpdate);

                    // Client Order, add if does not already exists
                    var attempts = 5;
                    ClientOrder clientOrder = new();
                    while (attempts > 0)
                    {
                        try
                        {
                            _logger.Debug($"Loading Client Order: {orderUpdate.ClientOrderId}");
                            clientOrder = database.ClientOrder.FirstOrDefault(c => c.Id == orderUpdate.ClientOrderId);

                            _logger.Debug($"Client Order {((clientOrder == null) ? "not" : string.Empty)} found: : {orderUpdate.ClientOrderId}");

                            if (clientOrder == null)
                            {
                                _logger.Information($"Adding new Client Order: {orderUpdate.ClientOrderId}");

                                clientOrder = new ClientOrder(orderUpdate.ClientOrderId)
                                {
                                    CreatedOn = DateTime.Now,
                                    Price = orderUpdate.Price,
                                    Quantity = orderUpdate.Quantity,
                                    Side = orderUpdate.Side,
                                    Type = orderUpdate.Type,
                                    TimeInForce = orderUpdate.TimeInForce,
                                    Symbol = orderUpdate.Symbol
                                };

                                database.ClientOrder.Add(clientOrder);

                                await database.SaveChangesAsync().ConfigureAwait(true);
                            }

                            break;
                        }
                        catch (Exception coe)
                        {
                            attempts--;

                            _logger.Warning($"Failed attempt to store Client Order {clientOrder.Id}; attempt: {attempts}");

                            database.Entry(clientOrder).State = EntityState.Modified;

                            try
                            {
                                await database.SaveChangesAsync().ConfigureAwait(true);
                                _logger.Warning($"Modified state accepted for: {clientOrder.Id}");
                            }
                            catch (Exception coe2)
                            {
                                _logger.Error(coe, $"Finally attempt to store Client Order {clientOrder.Id} failed");

                                _logger.Warning(coe2, $"Modified state not accepted for: {clientOrder.Id}");
                            }

                            if (attempts == 0)
                            {
                                throw;
                            }
                        }
                    }

                    // Find existing order list
                    var orderList = await database.OrderLists.FirstOrDefaultAsync(o => o.OrderListId == orderUpdate.OrderListId).ConfigureAwait(false);
                    if (orderList == null)
                    {
                        // Create new order list
                        orderList = new OrderList()
                        {
                            ContingencyType = string.Empty,
                            ListClientOrderId = orderUpdate.ClientOrderId,
                            ListStatusType = Datalayer.Enums.ListStatusType.ExecutionStarted,
                            ListOrderStatus = Datalayer.Enums.ListOrderStatus.Executing,
                            Symbol = orderUpdate.Symbol,
                            TransactionTime = DateTime.UtcNow
                        };

                        // Add orderlist
                        database.OrderLists.Add(orderList);
                    }

                    // Find existing order
                    var order = await database.Orders.FirstOrDefaultAsync(o => o.ClientOrderId == orderUpdate.ClientOrderId).ConfigureAwait(true);
                    if (order == null)
                    {
                        // Create new order
                        order = new Order()
                        {
                            OrderId = orderUpdate.OrderId,
                            ClientOrderId = orderUpdate.ClientOrderId,
                            Symbol = orderUpdate.Symbol,
                            CreatedOn = DateTime.UtcNow,
                            ClientOrder = clientOrder,
                            OrderList = orderList
                        };

                        // Add order
                        database.Orders.Add(order);
                    }
                    // Add order update to new/old order
                    order.OrderUpdates.Add(orderUpdate);
                    orderUpdate.Order = order;

                    // Add the order update
                    database.OrderUpdates.Add(orderUpdate);

                    // Add order to order list
                    if (!orderList.Orders.Contains(order))
                    {
                        orderList.Orders.Add(order);
                        order.OrderList = orderList;
                    }

                    // Add order update
                    orderList.OrderUpdates.Add(orderUpdate);
                    orderUpdate.OrderList = orderList;

                    // If sell and filled, remove quantity from buy
                    if (binanceStreamOrderUpdate.Side == OrderSide.Sell && binanceStreamOrderUpdate.Status == OrderStatus.Filled)
                    {
                        // Get trades where assets were bought
                        var buyTrades = database.PlacedOrders
                                        .Where(p => p.Side == Datalayer.Enums.OrderSide.Buy
                                            && p.Symbol == binanceStreamOrderUpdate.Symbol)
                                        .SelectMany(f => f.Fills.Where(f => f.Quantity > f.ConsumedQuantity
                                                                    && f.Price <= binanceStreamOrderUpdate.Price))
                                        .OrderByDescending(t => t.Price);

                        var soldQuantity = binanceStreamOrderUpdate.QuantityFilled;

                        // Fill trades with sold quantity
                        foreach (var trade in buyTrades)
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

                        // In case it was sold over price, remove from smallest over price first
                        if (soldQuantity != 0)
                        {
                            // Get trades where assets were bought
                            var overPriceBuyTrades = database.PlacedOrders
                                            .Where(p => p.Side == Datalayer.Enums.OrderSide.Buy
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

                    await database.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.ForContext(typeof(Accountant));
                _logger.Error(e, e.Message);
            }
            finally
            {
                _syncBinanceStreamOrderUpdate.Release();
            }

            // Clear blocked amount
            if (binanceStreamOrderUpdate.Status == OrderStatus.Filled
                || binanceStreamOrderUpdate.Status == OrderStatus.Rejected
                || binanceStreamOrderUpdate.Status == OrderStatus.Expired
                || binanceStreamOrderUpdate.Status == OrderStatus.Canceled)
            {
                _buffer.RemoveOpenOrder(binanceStreamOrderUpdate.ClientOrderId);
            }

            _logger.Verbose("Received Binance Stream Order Update");
        }

        /// <summary>
        /// Receives Binance order lists
        /// </summary>
        /// <param name="binanceStreamOrderList"></param>
        private async void SaveBinanceStreamOrderList(BinanceStreamOrderList binanceStreamOrderList)
        {
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    // Parse the new one
                    var newOrderList = Translator.Translate(binanceStreamOrderList);

                    // Check if there is an old one
                    var oldOrderList = await database.OrderLists.FirstOrDefaultAsync(o => o.OrderListId == newOrderList.OrderListId).ConfigureAwait(false);
                    if (oldOrderList == null)
                    {
                        // Add the new one
                        database.OrderLists.Add(newOrderList);
                    }
                    else
                    {
                        // Update the old one
                        oldOrderList.ContingencyType = newOrderList.ContingencyType;
                        oldOrderList.ListClientOrderId = newOrderList.ListClientOrderId;
                        oldOrderList.ListOrderStatus = newOrderList.ListOrderStatus;
                        oldOrderList.ListStatusType = newOrderList.ListStatusType;
                        oldOrderList.OrderListId = newOrderList.OrderListId;
                        oldOrderList.TransactionTime = newOrderList.TransactionTime;
                    }

                    await database.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.ForContext(typeof(Accountant));
                    _logger.Error(e, e.Message);
                }
            }

            _logger.Verbose("Received Binance Stream Order List");
        }

        /// <summary>
        /// Receives Binance balances
        /// </summary>
        /// <param name="binanceStreamPositionsUpdate"></param>
        private async void SaveBinanceStreamPositionUpdate(BinanceStreamPositionsUpdate binanceStreamPositionsUpdate)
        {
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    // Get account info, has to be present because is loaded already when Accountant is started.
                    var accountInfo = await database.AccountInfos.FirstAsync().ConfigureAwait(true);

                    var balances = Translator.Translate(binanceStreamPositionsUpdate);
                    foreach (var balance in balances)
                    {
                        var oldBalance = await database.Balances.FirstOrDefaultAsync(b => b.Asset == balance.Asset).ConfigureAwait(true);
                        if (oldBalance == null)
                        {
                            balance.AccountInfo = accountInfo;
                            accountInfo.Balances.Add(balance);
                            database.Balances.Add(balance);
                        }
                        else
                        {
                            oldBalance.Free = balance.Free;
                            oldBalance.Locked = balance.Locked;
                            oldBalance.Total = balance.Total;
                        }
                    }

                    await database.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.ForContext(typeof(Accountant));
                    _logger.Error(e, e.Message);
                }
            }

            _logger.Verbose("Received Binance Stream Balances");
        }

        /// <summary>
        /// Receives Binance balance updates
        /// </summary>
        /// <param name="binanceStreamBalanceUpdate"></param>
        private async void SaveBinanceStreamBalanceUpdate(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate)
        {
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    database.BalanceUpdates.AddRange(Translator.Translate(binanceStreamBalanceUpdate));
                    await database.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.ForContext(typeof(Accountant));
                    _logger.Error(e, e.Message);
                }
            }

            _logger.Verbose("Received Binance Stream Balance Update");
        }

        /// <summary>
        /// Returns the balance of an asset
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>Returns the balance or null if no balance available for the asset</returns>
        public async Task<BinanceBalance?> GetBalance(string asset)
        {
            // Take reference to original instance in case _binanceAccountInfo is updated
            var bac = await GetAccountInfo().ConfigureAwait(true);

            return bac?.Balances.FirstOrDefault(b => b.Asset == asset);
        }

        /// <summary>
        /// Returns the latest account information
        /// </summary>
        /// <returns></returns>
        public async Task<BinanceAccountInfo> GetAccountInfo()
        {
            var bac = _binanceAccountInfo;

            if (bac == null || _binanceAccountInfoUpdated < DateTime.UtcNow.AddSeconds(-6))
            {
                var accountInfoRequest = await _binanceClient.General.GetAccountInfoAsync(ct: _cancellationTokenSource.Token).ConfigureAwait(true);

                if (accountInfoRequest.Success)
                {
                    _binanceAccountInfo = accountInfoRequest.Data;
                    _binanceAccountInfoUpdated = DateTime.UtcNow;

                    _logger.Information("Requested account info");
                }
                else
                {
                    // Something is oddly wrong, wait a bit
                    _logger.Warning("Cannot retrieve account info");
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }

            // Save account info
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    // Check if account info is present
                    var accountInfo = await database.AccountInfos.FirstOrDefaultAsync().ConfigureAwait(false);
                    if (accountInfo == null)
                    {
                        // Add new account info
                        accountInfo = new AccountInfo()
                        {
                            BuyerCommission = _binanceAccountInfo.BuyerCommission,
                            CanDeposit = _binanceAccountInfo.CanDeposit,
                            CanTrade = _binanceAccountInfo.CanTrade,
                            CanWithdraw = _binanceAccountInfo.CanWithdraw,
                            MakerCommission = _binanceAccountInfo.MakerCommission,
                            SellerCommission = _binanceAccountInfo.SellerCommission,
                            TakerCommission = _binanceAccountInfo.TakerCommission,
                            UpdatedOn = _binanceAccountInfo.UpdateTime.ToUniversalTime()
                        };

                        database.AccountInfos.Add(accountInfo);

                        // Add balances
                        foreach (var balance in _binanceAccountInfo.Balances)
                        {
                            accountInfo.Balances.Add(Translator.Translate(balance, accountInfo));
                        }
                    }
                    else
                    {
                        // Update existing account info
                        accountInfo.BuyerCommission = _binanceAccountInfo.BuyerCommission;
                        accountInfo.CanDeposit = _binanceAccountInfo.CanDeposit;
                        accountInfo.CanTrade = _binanceAccountInfo.CanTrade;
                        accountInfo.CanWithdraw = _binanceAccountInfo.CanWithdraw;
                        accountInfo.MakerCommission = _binanceAccountInfo.MakerCommission;
                        accountInfo.SellerCommission = _binanceAccountInfo.SellerCommission;
                        accountInfo.TakerCommission = _binanceAccountInfo.TakerCommission;
                        accountInfo.UpdatedOn = _binanceAccountInfo.UpdateTime.ToUniversalTime();

                        // Add or update balances
                        foreach (var newBalance in _binanceAccountInfo.Balances)
                        {
                            var oldBalance = accountInfo.Balances.Find(b => b.Asset == newBalance.Asset);
                            if (oldBalance == null)
                            {
                                accountInfo.Balances.Add(Translator.Translate(newBalance, accountInfo));
                            }
                            else
                            {
                                oldBalance.Free = newBalance.Free;
                                oldBalance.Locked = newBalance.Locked;
                                oldBalance.Total = newBalance.Total;
                            }
                        }
                    }

                    // Save changes
                    await database.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.ForContext(typeof(Accountant));
                    _logger.Error(e, e.Message);
                }
            }

            return _binanceAccountInfo;
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Starts the <c>Accountant</c>
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            _logger.Verbose("Starting Accountant");

            // Connect to user stream
            var result = await _binanceClient.Spot.UserStream.StartUserStreamAsync(ct: _cancellationTokenSource.Token).ConfigureAwait(true);
            if (result.Success)
            {
                _binanceListenKey = result.Data;
                _logger.Verbose("Connection ok, ListenKey received");
            }

            // Run initial keep alive
            ConnectionKeepAlive();

            // Subscribe to socket events
            await _binanceSocketClient.Spot.SubscribeToUserDataUpdatesAsync(_binanceListenKey,
                SaveBinanceStreamAccountInfo,
                SaveBinanceStreamOrderUpdate,
                SaveBinanceStreamOrderList,
                SaveBinanceStreamPositionUpdate,
                SaveBinanceStreamBalanceUpdate).ConfigureAwait(true);

            _logger.Information("Binance Client is online");

            // Get updated account infos
            _ = await GetAccountInfo().ConfigureAwait(false);

            // Start jobs
            _jobSynchronizeAccountInfo.Start();
            _jobConnectionKeepAlive.Start();

            _logger.Debug("Accountant started");
        }

        /// <summary>
        /// Stops the <c>Accountant</c>
        /// </summary>
        /// <returns></returns>
        public async Task Terminate()
        {
            _logger.Verbose("Stopping accountant");

            // Stop jobs
            _jobSynchronizeAccountInfo.Terminate();
            _jobConnectionKeepAlive.Terminate();

            await _binanceClient.Spot.UserStream.StopUserStreamAsync(_binanceListenKey).ConfigureAwait(true);

            // Wait until delay elapsed event finishes to give background tasks some time and buffers time to flush
            await Task.Delay(1000).ConfigureAwait(true);

            // Signal cancellation for what ever remains
            _cancellationTokenSource.Cancel();

            _logger.Debug("Accountant stopped");
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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _jobSynchronizeAccountInfo.Dispose();
                _binanceClient.Dispose();
                _syncBinanceStreamOrderUpdate.Dispose();
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
