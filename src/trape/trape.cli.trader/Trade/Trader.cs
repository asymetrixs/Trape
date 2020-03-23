using Binance.Net.Interfaces;
using Binance.Net.Objects;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;

namespace trape.cli.trader.trade
{
    public class Trader : ITrader
    {
        #region Fields

        private bool _disposed;

        private ILogger _logger;

        private IAccountant _accountant;

        private IRecommender _recommender;
        
        private IBuffer _buffer;

        private System.Timers.Timer _timerTrading;

        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Constructor

        public Trader(ILogger logger, IAccountant accountant, IRecommender recommender, IBuffer buffer)
        {
            if (null == logger || null == accountant || null == recommender || null == buffer)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._accountant = accountant;
            this._recommender = recommender;
            this._buffer = buffer;
            this._cancellationTokenSource = new CancellationTokenSource();

            this._timerTrading = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 1).TotalMilliseconds
            };
            this._timerTrading.Elapsed += _timerTrading_Elapsed;
        }

        #endregion

        #region Timer Elapsed

        private async void _timerTrading_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();

            // Quote Order Qty Market orders have been enabled on all symbols.
            // Quote Order Qty MARKET orders allow a user to specify the total quoteOrderQty spent or received in the MARKET order.
            // Quote Order Qty MARKET orders will not break LOT_SIZE filter rules; the order will execute a quantity that will have the notional value as close as possible to quoteOrderQty.
            // Using BNBBTC as an example:
            // On the BUY side, the order will buy as many BNB as quoteOrderQty BTC can.
            // On the SELL side, the order will sell as much BNB as needed to receive quoteOrderQty BTC.

            // Using BTCUSDT as an example:
            // On the BUY side, the order will buy as many BTC as quoteOrderQty USDT can.
            // On the SELL side, the order will sell as much BTCas needed to receive quoteOrderQty USDT.


            foreach (var symbol in this._buffer.GetSymbols())
            {
                var recommendation = this._recommender.GetRecommendation(symbol);
                if(null == recommendation)
                {
                    continue;
                }

                var usdt = this._accountant.GetBalance("USDT");
                if (null == usdt || usdt.Free < 2)
                {
                    this._logger.Debug($"Skipping order as USDT does not have enough free resources {usdt?.Free}");
                    return;
                }

                // Only take half of what is available
                var availableAmount = usdt.Free / 2;
                
                var currentPrices = await database.GetCurrentPrice(this._cancellationTokenSource.Token).ConfigureAwait(false);
                var currentSymbolPrice = currentPrices.SingleOrDefault(cp => cp.Symbol == symbol);
                

                if (null == currentSymbolPrice)
                {
                    this._logger.Debug($"Skipping order as {symbol} does not have a latest price");
                    return;
                }

                var assetBalance = this._accountant.GetBalance(symbol.Replace("USDT", string.Empty));
                // Sell 66% of the asset
                var assetBalanceForSale = assetBalance.Free * 0.66M;

                var clientOrderId = Guid.NewGuid().ToString("N");
                var binanceClient = Program.Services.GetService(typeof(IBinanceClient)) as IBinanceClient;

                if(recommendation.Action == Analyze.Action.Buy)
                {
                    await binanceClient.PlaceOrderAsync(symbol, OrderSide.Buy, OrderType.Limit,
                        quoteOrderQuantity: availableAmount, newClientOrderId: clientOrderId, orderResponseType: OrderResponseType.Full,
                        timeInForce: TimeInForce.ImmediateOrCancel, ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
                }
                else if(recommendation.Action == Analyze.Action.Sell)
                {
                    await binanceClient.PlaceOrderAsync(symbol, OrderSide.Buy, OrderType.Limit,
                        quoteOrderQuantity: availableAmount, newClientOrderId: clientOrderId, orderResponseType: OrderResponseType.Full,
                        timeInForce: TimeInForce.ImmediateOrCancel, ct: this._cancellationTokenSource.Token).ConfigureAwait(false);
                    this._logger.Information($"Issued order to sell {symbol} ");
                }
                else
                {

                }


            }
            Pool.DatabasePool.Put(database);
        }

        

        #endregion

    #region Start / Stop

        public async Task Start()
        {
            this._logger.Information("Starting Trader");

            throw new NotImplementedException();

            

            this._timerTrading.Start();

            this._logger.Information("Trader started");
        }

        public void Stop()
        {
            this._logger.Information("Stopping Trader");

            this._timerTrading.Stop();

            throw new NotImplementedException();

            this._logger.Information("Trader stopped");
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
                this._timerTrading.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
