using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using Binance.Net.Objects.Sockets;
using CryptoExchange.Net.Authentication;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Analyze;

namespace trape.cli.trader.Account
{
    public class Accountant : IAccountant
    {
        #region Fields

        private bool _disposed;

        private System.Timers.Timer _timerSynchronizeAccountInfo;

        private System.Timers.Timer _timerConnectionKeepAlive;

        private IBinanceClient _binanceClient;

        private IBinanceSocketClient _binanceSocketClient;

        private BinanceAccountInfo _binanceAccountInfo;

        private BinanceStreamAccountInfo _binanceStreamAccountInfo;

        private ILogger _logger;

        private CancellationTokenSource _cancellationTokenSource;

        private string _binanceListenKey;

        private IRecommender _recommender;

        #endregion

        #region Constructor

        public Accountant(ILogger logger, IRecommender recommender)
        {
            this._logger = logger;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._recommender = recommender;

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

        private async void _timerSynchronizeAccountInfo_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Get latest account information
            var accountInfo = await this._binanceClient.GetAccountInfoAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
            if (accountInfo.Success)
            {
                this._binanceAccountInfo = accountInfo.Data;
            }

            this._logger.Debug("Account info synchronized");
        }

        #endregion

        private async Task Order(string symbol)
        {
            this._logger.Information("Placing test order");

            
        }
        
        public BinanceBalance GetBalance(string asset)
        {
            // Take reference to original instance in case _binanceAccountInfo is updated
            var bac = this._binanceAccountInfo;

            return bac.Balances.SingleOrDefault(b => b.Asset == asset);
        }

        #region Start / Stop

        public async System.Threading.Tasks.Task Start()
        {
            this._logger.Verbose("Starting Accountant");

            // Create new binance client
            this._binanceClient = Program.Services.GetService(typeof(IBinanceClient)) as IBinanceClient;

            // Connect to user stream
            var result = await this._binanceClient.StartUserStreamAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
            if (result.Success)
            {
                this._binanceListenKey = result.Data;
                this._logger.Verbose("Connection ok, ListenKey received");
            }

            // Run initial timer elapsed event
            _timerConnectionKeepAlive_Elapsed(null, null);

            // Create new binance socket client
            this._binanceSocketClient = Program.Services.GetService(typeof(IBinanceSocketClient)) as IBinanceSocketClient;

            // Subscribe to socket events
            await this._binanceSocketClient.SubscribeToUserDataUpdatesAsync(this._binanceListenKey,
                (bsai) => _saveBinanceStreamAccountInfo(bsai),
                (bsou) => _saveBinanceStreamOrderUpdate(bsou),
                (bsol) => _saveBinanceStreamOrderList(bsol),
                (bsbs) => _saveBinanceStreamBalance(bsbs),
                (bsbu) => _saveBinanceStreamBalanceUpdate(bsbu)
                ).ConfigureAwait(false);

            this._logger.Information("Binance Client is online");

            // Start timer
            this._timerSynchronizeAccountInfo.Start();

            this._logger.Debug("Accountant started");
        }

        private void _saveBinanceStreamAccountInfo(BinanceStreamAccountInfo binanceStreamAccountInfo)
        {
            this._binanceStreamAccountInfo = binanceStreamAccountInfo;
            this._logger.Verbose("Received Binance Stream Account Info");
        }

        private async void _saveBinanceStreamOrderUpdate(BinanceStreamOrderUpdate binanceStreamOrderUpdate)
        {
            var database = Pool.DatabasePool.Get();
            await database.InsertAsync(binanceStreamOrderUpdate, this._cancellationTokenSource.Token).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Received Binance Stream Order Update");
        }

        private async void _saveBinanceStreamOrderList(BinanceStreamOrderList binanceStreamOrderList)
        {
            var database = Pool.DatabasePool.Get();
            await database.InsertAsync(binanceStreamOrderList, this._cancellationTokenSource.Token).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Received Binance Stream Order List");
        }

        private async void _saveBinanceStreamBalance(IEnumerable<BinanceStreamBalance> binanceStreamBalances)
        {
            var database = Pool.DatabasePool.Get();
            await database.InsertAsync(binanceStreamBalances, this._cancellationTokenSource.Token).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Received Binance Stream Balances");
        }

        private async void _saveBinanceStreamBalanceUpdate(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate)
        {
            var database = Pool.DatabasePool.Get();
            await database.InsertAsync(binanceStreamBalanceUpdate, this._cancellationTokenSource.Token).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Received Binance Stream Balance Update");
        }

        public void Stop()
        {
            this._logger.Verbose("Stopping accountant");

            // Stop timer
            this._timerSynchronizeAccountInfo.Stop();

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
