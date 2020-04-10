using Binance.Net.Interfaces;
using Binance.Net.Objects;
using Binance.Net.Objects.Sockets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly System.Timers.Timer _timerSynchronizeAccountInfo;

        /// <summary>
        /// Binance connection keepalive
        /// </summary>
        private readonly System.Timers.Timer _timerConnectionKeepAlive;

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
        /// Stream account info
        /// </summary>
        private BinanceStreamAccountInfo _binanceStreamAccountInfo;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

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
        public Accountant(ILogger logger, IBinanceClient binanceClient, IBinanceSocketClient binanceSocketClient)
        {
            if (logger == null || binanceClient == null || binanceSocketClient == null)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger.ForContext<Accountant>();
            this._cancellationTokenSource = new CancellationTokenSource();
            this._binanceClient = binanceClient;
            this._binanceSocketClient = binanceSocketClient;

            #region Timer Setup

            // Create timer for account info synchronization
            this._timerSynchronizeAccountInfo = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 5).TotalMilliseconds
            };
            this._timerSynchronizeAccountInfo.Elapsed += _timerSynchronizeAccountInfo_Elapsed;

            // Create timer for connection keep alive
            this._timerConnectionKeepAlive = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 15, 0).TotalMilliseconds
            };
            this._timerConnectionKeepAlive.Elapsed += _timerConnectionKeepAlive_Elapsed;

            #endregion
        }

        #endregion

        #region Timer Elapsed

        /// <summary>
        /// Sends ping to binance to keep the connection alive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timerConnectionKeepAlive_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
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
        private async void _timerSynchronizeAccountInfo_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Get latest account information
            var accountInfo = await this._binanceClient.GetAccountInfoAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
            if (accountInfo.Success)
            {
                this._binanceAccountInfo = accountInfo.Data;
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
            var database = Pool.DatabasePool.Get();
            await database.InsertAsync(binanceStreamOrderUpdate, this._cancellationTokenSource.Token).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Received Binance Stream Order Update");
        }

        /// <summary>
        /// Receives Binance order lists
        /// </summary>
        /// <param name="binanceStreamOrderList"></param>
        private async void _saveBinanceStreamOrderList(BinanceStreamOrderList binanceStreamOrderList)
        {
            var database = Pool.DatabasePool.Get();
            await database.InsertAsync(binanceStreamOrderList, this._cancellationTokenSource.Token).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Received Binance Stream Order List");
        }

        /// <summary>
        /// Receives Binance balances
        /// </summary>
        /// <param name="binanceStreamBalances"></param>
        private async void _saveBinanceStreamBalance(IEnumerable<BinanceStreamBalance> binanceStreamBalances)
        {
            var database = Pool.DatabasePool.Get();
            await database.InsertAsync(binanceStreamBalances, this._cancellationTokenSource.Token).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Received Binance Stream Balances");
        }

        /// <summary>
        /// Receives Binance balance updates
        /// </summary>
        /// <param name="binanceStreamBalanceUpdate"></param>
        private async void _saveBinanceStreamBalanceUpdate(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate)
        {
            var database = Pool.DatabasePool.Get();
            await database.InsertAsync(binanceStreamBalanceUpdate, this._cancellationTokenSource.Token).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);

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

        public async Task<BinanceAccountInfo> GetAccountInfo()
        {
            var bac = this._binanceAccountInfo;

            if (null == bac || this._binanceAccountInfo.UpdateTime < DateTime.UtcNow.AddSeconds(-3))
            {
                var accountInfoRequest = await this._binanceClient.GetAccountInfoAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(true);

                if (accountInfoRequest.Success)
                {
                    this._binanceAccountInfo = accountInfoRequest.Data;

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

            // Run initial timer elapsed event
            this._timerConnectionKeepAlive_Elapsed(null, null);

            // Subscribe to socket events
            await this._binanceSocketClient.SubscribeToUserDataUpdatesAsync(this._binanceListenKey,
                (bsai) => _saveBinanceStreamAccountInfo(bsai),
                (bsou) => _saveBinanceStreamOrderUpdate(bsou),
                (bsol) => _saveBinanceStreamOrderList(bsol),
                (bsbs) => _saveBinanceStreamBalance(bsbs),
                (bsbu) => _saveBinanceStreamBalanceUpdate(bsbu)
                ).ConfigureAwait(true);

            this._logger.Information("Binance Client is online");

            // Start timer
            this._timerSynchronizeAccountInfo.Start();

            this._logger.Debug("Accountant started");
        }

        /// <summary>
        /// Stops the <c>Accountant</c>
        /// </summary>
        /// <returns></returns>
        public async Task Finish()
        {
            this._logger.Verbose("Stopping accountant");

            // Stop timer
            this._timerSynchronizeAccountInfo.Stop();

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
                this._timerSynchronizeAccountInfo.Dispose();
                this._binanceClient.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
