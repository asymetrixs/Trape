using Binance.Net;
using Binance.Net.Objects;
using Binance.Net.Objects.Sockets;
using CryptoExchange.Net.Authentication;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace trape.cli.trader.Account
{
    public class Accountant : IAccountant, IDisposable
    {
        private bool _disposed;

        private Timer _timerSynchronizeAccountInfo;

        private Timer _timerConnectionKeepAlive;

        private BinanceClient _binanceClient;

        private BinanceSocketClient _binanceSocketClient;

        private BinanceAccountInfo _binanceAccountInfo;

        private BinanceStreamAccountInfo _binanceStreamAccountInfo;

        private ILogger _logger;

        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        private string _binanceListenKey;

        public Accountant(ILogger logger)
        {
            this._logger = logger;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();

            // Create timer for account info synchronization
            this._timerSynchronizeAccountInfo = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 5).TotalMilliseconds
            };
            this._timerSynchronizeAccountInfo.Elapsed += _timerSynchronizeAccountInfo_Elapsed;

            // Create timer for connection keep alive
            this._timerConnectionKeepAlive = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 15, 0).TotalMilliseconds
            };
            this._timerConnectionKeepAlive.Elapsed += _timerConnectionKeepAlive_Elapsed;
        }

        private async void _timerConnectionKeepAlive_Elapsed(object sender, ElapsedEventArgs e)
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

        private async void _timerSynchronizeAccountInfo_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Get latest account information
            var accountInfo = await this._binanceClient.GetAccountInfoAsync(ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
            if (accountInfo.Success)
            {
                this._binanceAccountInfo = accountInfo.Data;
            }

            this._logger.Debug("Account info synchronized");
        }

        private void Order()
        {
            var orderId = Guid.NewGuid().ToString("N");
            this._logger.Information("Placing test order");

            // Quote Order Qty Market orders have been enabled on all symbols.
            // Quote Order Qty MARKET orders allow a user to specify the total quoteOrderQty spent or received in the MARKET order.
            // Quote Order Qty MARKET orders will not break LOT_SIZE filter rules; the order will execute a quantity that will have the notional value as close as possible to quoteOrderQty.
            // Using BNBBTC as an example:
            // On the BUY side, the order will buy as many BNB as quoteOrderQty BTC can.
            // On the SELL side, the order will sell as much BNB as needed to receive quoteOrderQty BTC.

            var testOrder = this._binanceClient.PlaceTestOrder(
                symbol: "BTCUSDT",
                side: OrderSide.Buy,
                type: OrderType.Limit,
                price: 6000,
                quantity: 1,
                newClientOrderId: orderId,
                timeInForce: TimeInForce.ImmediateOrCancel,
                ct: this._cancellationTokenSource.Token);
        }

        public decimal? AvailableCredit(string symbol)
        {
            // Take reference to original instance in case _binanceAccountInfo is updated
            var bac = this._binanceAccountInfo;

            return bac.Balances.SingleOrDefault(b => b.Asset == symbol)?.Free;
        }

        public async System.Threading.Tasks.Task Start()
        {
            this._logger.Verbose("Starting Accountant");

            // Create new binance client
            this._binanceClient = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(Configuration.GetValue("binance:apikey"),
                                                        Configuration.GetValue("binance:secretkey")),
            });

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
            this._binanceSocketClient = new BinanceSocketClient(new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials(Configuration.GetValue("binance:apikey"),
                                                        Configuration.GetValue("binance:secretkey")),
                AutoReconnect = true
            });

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
        }

        private void _saveBinanceStreamOrderUpdate(BinanceStreamOrderUpdate binanceStreamOrderUpdate)
        {

        }

        private void _saveBinanceStreamOrderList(BinanceStreamOrderList binanceStreamOrderList)
        {

        }

        private void _saveBinanceStreamBalance(IEnumerable<BinanceStreamBalance> binanceStreamBalances)
        {

        }

        private void _saveBinanceStreamBalanceUpdate(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate)
        {

        }

        public void Stop()
        {
            this._logger.Verbose("Stopping accountant");

            // Stop timer
            this._timerSynchronizeAccountInfo.Stop();

            this._logger.Debug("Accountant stopped");
        }

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
    }
}
