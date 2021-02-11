namespace Trape.Cli.Trader.Account
{
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

    /// <summary>
    /// Implementation of <c>IAccountant</c> managing the binance inventory
    /// </summary>
    public class Accountant : IAccountant
    {
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
        /// Synchronize stream order updates
        /// </summary>
        private readonly SemaphoreSlim _syncBinanceStreamOrderUpdate;

        /// <summary>
        /// Trade summaries
        /// </summary>
        private readonly ConcurrentDictionary<string, TradeSummary> _tradeSummaries;

        /// <summary>
        /// Binance listen key
        /// </summary>
        private string _binanceListenKey;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Account Info
        /// </summary>
        private BinanceAccountInfo? _binanceAccountInfo;

        /// <summary>
        /// Last time the binance account info was updated
        /// </summary>
        private DateTime _binanceAccountInfoUpdated;

        /// <summary>
        /// Binance Account Info
        /// </summary>
        private BinanceStreamAccountInfo? _binanceStreamAccountInfo;

        /// <summary>
        /// Initializes a new instance of the <c>Accountant</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="cache">Cache</param>
        /// <param name="binanceClient">Binance Client</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public Accountant(ILogger logger, ICache cache, IBinanceClient binanceClient, IBinanceSocketClient binanceSocketClient)
        {
            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._cache = cache ?? throw new ArgumentNullException(paramName: nameof(cache));

            this._binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            this._binanceSocketClient = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            this._logger = logger.ForContext<Accountant>();
            this._cancellationTokenSource = new CancellationTokenSource();
            this._binanceAccountInfoUpdated = default;
            this._syncBinanceStreamOrderUpdate = new SemaphoreSlim(1, 1);
            this._tradeSummaries = new ConcurrentDictionary<string, TradeSummary>();
            this._binanceListenKey = string.Empty;
            this._binanceAccountInfo = null;
            this._binanceStreamAccountInfo = null;

            // Create timer for account info synchronization
            this._jobSynchronizeAccountInfo = new Job(new TimeSpan(0, 0, 5), async () => await this.SynchronizeAccountInfo().ConfigureAwait(true));

            // Create timer for connection keep alive
            this._jobConnectionKeepAlive = new Job(new TimeSpan(0, 15, 0), this.ConnectionKeepAlive);
        }

        /// <summary>
        /// Returns the balance of an asset
        /// </summary>
        /// <param name="asset">Asset</param>
        /// <returns>Returns the balance or null if no balance available for the asset</returns>
        public async Task<BinanceBalance?> GetBalance(string asset)
        {
            // Take reference to original instance in case _binanceAccountInfo is updated
            var bac = await this.GetAccountInfo().ConfigureAwait(true);

            return bac?.Balances.FirstOrDefault(b => b.Asset == asset);
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
                await this.SynchronizeAccountInfo().ConfigureAwait(true);
            }

            return this._binanceAccountInfo;
        }

        /// <summary>
        /// Returns trade summary for symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public TradeSummary? GetTradeSummary(string symbol)
        {
            _ = this._tradeSummaries.TryGetValue(symbol, out TradeSummary? value);

            return value;
        }

        /// <summary>
        /// Starts the <c>Accountant</c>
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            this._logger.Verbose("Starting Accountant");

            // Connect to user stream
            var result = await this._binanceClient.Spot.UserStream.StartUserStreamAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(true);
            if (result.Success)
            {
                this._binanceListenKey = result.Data;
                this._logger.Verbose("Connection ok, ListenKey received");
            }

            // Run initial keep alive
            this.ConnectionKeepAlive();

            // Subscribe to socket events
            await this._binanceSocketClient.Spot.SubscribeToUserDataUpdatesAsync(this._binanceListenKey,
                this.SaveBinanceStreamAccountInfo,
                this.SaveBinanceStreamOrderUpdate,
                this.SaveBinanceStreamOrderList,
                this.SaveBinanceStreamPositionUpdate,
                this.SaveBinanceStreamBalanceUpdate).ConfigureAwait(true);

            this.
                _logger.Information("Binance Client is online");

            // Get updated account infos
            _ = await this.GetAccountInfo().ConfigureAwait(false);

            // Start jobs
            this._jobSynchronizeAccountInfo.Start();
            this._jobConnectionKeepAlive.Start();

            this._logger.Debug("Accountant started");
        }

        /// <summary>
        /// Stops the <c>Accountant</c>
        /// </summary>
        /// <returns></returns>
        public async Task Terminate()
        {
            this._logger.Verbose("Stopping accountant");

            // Stop jobs
            this._jobSynchronizeAccountInfo.Terminate();
            this._jobConnectionKeepAlive.Terminate();

            await this._binanceClient.Spot.UserStream.StopUserStreamAsync(this._binanceListenKey).ConfigureAwait(true);

            // Wait until delay elapsed event finishes to give background tasks some time and buffers time to flush
            await Task.Delay(1000).ConfigureAwait(true);

            // Signal cancellation for what ever remains
            this._cancellationTokenSource.Cancel();

            this._logger.Debug("Accountant stopped");
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">Disposing</param>
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
                this._syncBinanceStreamOrderUpdate.Dispose();
                this._cancellationTokenSource.Dispose();
                this._jobConnectionKeepAlive.Dispose();
            }

            this._disposed = true;
        }

        /// <summary>
        /// Sends ping to binance to keep the connection alive
        /// </summary>
        private async void ConnectionKeepAlive()
        {
            // Test connection to binance
            var ping = await this._binanceClient.PingAsync(this._cancellationTokenSource.Token).ConfigureAwait(false);

            if (ping.Success)
            {
                this._logger.Verbose("Ping successful");

                // Send keep alive packages
                var userStreamKeepAlive = await this._binanceClient.Spot.UserStream.KeepAliveUserStreamAsync(this._binanceListenKey, ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
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
        private async Task SynchronizeAccountInfo()
        {
            // Get latest account information
            var accountInfo = await this._binanceClient.General.GetAccountInfoAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
            if (accountInfo.Success)
            {
                this._binanceAccountInfo = accountInfo.Data;
                this._binanceAccountInfoUpdated = DateTime.UtcNow;
            }

            this._logger.Verbose("Account info synchronized");
        }

        /// <summary>
        /// Receives Binance account info
        /// </summary>
        /// <param name="binanceStreamAccountInfo">Binance Account Info</param>
        private void SaveBinanceStreamAccountInfo(BinanceStreamAccountInfo binanceStreamAccountInfo)
        {
            this._binanceStreamAccountInfo = binanceStreamAccountInfo;
            this._logger.Verbose("Received Binance Stream Account Info");
        }

        /// <summary>
        /// Receives Binance order updates
        /// </summary>
        /// <param name="binanceStreamOrderUpdate">Binance Order Update</param>
        private void SaveBinanceStreamOrderUpdate(BinanceStreamOrderUpdate binanceStreamOrderUpdate)
        {
            // Clear blocked amount
            if (binanceStreamOrderUpdate.Status == OrderStatus.Filled
                || binanceStreamOrderUpdate.Status == OrderStatus.Rejected
                || binanceStreamOrderUpdate.Status == OrderStatus.Expired
                || binanceStreamOrderUpdate.Status == OrderStatus.Canceled)
            {
                this._cache.RemoveOpenOrder(binanceStreamOrderUpdate.ClientOrderId);
            }

            // Run trade summary update task on thread pool
            Task.Run(() =>
            {
                var order = this._binanceClient.Spot.Order.GetMyTrades(binanceStreamOrderUpdate.Symbol);

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

                this._tradeSummaries[binanceStreamOrderUpdate.Symbol] = new TradeSummary(binanceStreamOrderUpdate.Symbol, quantity, quoteQuantity);
            });

            this._logger.Verbose("Received Binance Stream Order Update");
        }

        /// <summary>
        /// Receives Binance order lists
        /// </summary>
        /// <param name="binanceStreamOrderList">Binance Order List</param>
        private void SaveBinanceStreamOrderList(BinanceStreamOrderList binanceStreamOrderList)
        {
            this._logger.Verbose("Received Binance Stream Order List");
        }

        /// <summary>
        /// Receives Binance balances
        /// </summary>
        /// <param name="binanceStreamPositionsUpdate">Binance Positions Update</param>
        private void SaveBinanceStreamPositionUpdate(BinanceStreamPositionsUpdate binanceStreamPositionsUpdate)
        {
            this._logger.Verbose("Received Binance Stream Balances");
        }

        /// <summary>
        /// Receives Binance balance updates
        /// </summary>
        /// <param name="binanceStreamBalanceUpdate">Binance Balance Update</param>
        private void SaveBinanceStreamBalanceUpdate(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate)
        {
            this._logger.Verbose("Received Binance Stream Balance Update");
        }
    }
}
