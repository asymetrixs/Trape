using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.SpotData;
using Binance.Net.Objects.Spot.UserStream;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.Trader.Account.Models;
using Trape.Cli.Trader.Cache;
using Trape.Jobs;

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
        /// Cache
        /// </summary>
        private readonly ICache _cache;

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

        /// <summary>
        /// Trade summaries
        /// </summary>
        private readonly ConcurrentDictionary<string, TradeSummary> _tradeSummaries;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Accountant</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="cache">Cache</param>
        /// <param name="binanceClient">Binance Client</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public Accountant(ILogger logger, ICache cache, IBinanceClient binanceClient, IBinanceSocketClient binanceSocketClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _cache = cache ?? throw new ArgumentNullException(paramName: nameof(cache));

            _binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            _binanceSocketClient = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            #endregion

            _logger = logger.ForContext<Accountant>();
            _cancellationTokenSource = new CancellationTokenSource();
            _binanceAccountInfoUpdated = default;
            _syncBinanceStreamOrderUpdate = new SemaphoreSlim(1, 1);
            _tradeSummaries = new ConcurrentDictionary<string, TradeSummary>();

            #region Job Setup

            // Create timer for account info synchronization
            _jobSynchronizeAccountInfo = new Job(new TimeSpan(0, 0, 5), async () => await SynchronizeAccountInfo().ConfigureAwait(true));

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
        private async Task SynchronizeAccountInfo()
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
        private void SaveBinanceStreamOrderUpdate(BinanceStreamOrderUpdate binanceStreamOrderUpdate)
        {
            // Clear blocked amount
            if (binanceStreamOrderUpdate.Status == OrderStatus.Filled
                || binanceStreamOrderUpdate.Status == OrderStatus.Rejected
                || binanceStreamOrderUpdate.Status == OrderStatus.Expired
                || binanceStreamOrderUpdate.Status == OrderStatus.Canceled)
            {
                _cache.RemoveOpenOrder(binanceStreamOrderUpdate.ClientOrderId);
            }

            // Run trade summary update task on thread pool
            Task.Run(() =>
            {
                var order = _binanceClient.Spot.Order.GetMyTrades(binanceStreamOrderUpdate.Symbol);

                if (!order.Success)
                {
                    return;
                }

                decimal quoteQuantity = 0;
                decimal quantity = 0;

                var tradesSorted = order.Data.OrderBy(o => o.TradeTime).ToArray();

                // Calculate quantities by following the trade history
                for (int i = 0; i < tradesSorted.Length; i++)
                {
                    var current = tradesSorted[i];
                    if (current.IsBuyer)
                    {
                        quantity += current.Quantity;
                        quoteQuantity += current.QuoteQuantity;
                    }
                    else
                    {
                        quantity -= current.Quantity;
                        quoteQuantity -= current.QuoteQuantity;
                    }
                }

                _tradeSummaries[binanceStreamOrderUpdate.Symbol] = new TradeSummary()
                {
                    Quantity = quantity,
                    QuoteQuantity = quoteQuantity,
                    Symbol = binanceStreamOrderUpdate.Symbol,
                };
            });

            _logger.Verbose("Received Binance Stream Order Update");
        }

        /// <summary>
        /// Receives Binance order lists
        /// </summary>
        /// <param name="binanceStreamOrderList"></param>
        private void SaveBinanceStreamOrderList(BinanceStreamOrderList binanceStreamOrderList)
        {
            _logger.Verbose("Received Binance Stream Order List");
        }

        /// <summary>
        /// Receives Binance balances
        /// </summary>
        /// <param name="binanceStreamPositionsUpdate"></param>
        private void SaveBinanceStreamPositionUpdate(BinanceStreamPositionsUpdate binanceStreamPositionsUpdate)
        {
            _logger.Verbose("Received Binance Stream Balances");
        }

        /// <summary>
        /// Receives Binance balance updates
        /// </summary>
        /// <param name="binanceStreamBalanceUpdate"></param>
        private void SaveBinanceStreamBalanceUpdate(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate)
        {
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
                await SynchronizeAccountInfo().ConfigureAwait(true);
            }

            return _binanceAccountInfo;
        }

        /// <summary>
        /// Returns trade summary for symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public TradeSummary? GetTradeSummary(string symbol)
        {
            if (_tradeSummaries.TryGetValue(symbol, out TradeSummary value))
            {
                return value;
            }

            return null;
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
                _jobConnectionKeepAlive.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
