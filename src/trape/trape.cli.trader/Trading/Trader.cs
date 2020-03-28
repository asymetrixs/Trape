using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Objects;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.DataLayer.Models;

namespace trape.cli.trader.Trading
{
    public class Trader : ITrader
    {
        #region Fields

        private bool _disposed;

        private readonly ILogger _logger;

        private IAccountant _accountant;

        private IRecommender _recommender;

        private IBuffer _buffer;

        private System.Timers.Timer _timerTrading;

        public string Symbol { get; private set; }

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
            this.Symbol = null;

            this._timerTrading = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 5).TotalMilliseconds
            };
            this._timerTrading.Elapsed += _timerTrading_Elapsed;
        }

        #endregion

        #region Timer Elapsed

        private async void _timerTrading_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Check if Symbol is set
            if (string.IsNullOrEmpty(this.Symbol))
            {
                this._logger.Error("Trying to trade with empty symbol! Aborting...");
                return;
            }

            this._logger.Verbose($"{this.Symbol}: Going to trade");

            // Get recommendation what to do
            var recommendation = this._recommender.GetRecommendation(this.Symbol);
            
            if (null == recommendation || recommendation.Action == Analyze.Action.Wait)
            {
                this._logger.Verbose($"{this.Symbol}: Waiting for recommendation");
                return;
            }

            // Get min notional
            var exchangeInfo = this._getExchangeInfo();
            var minNotional = exchangeInfo.MinNotionalFilter.MinNotional;

            // Generate new client order id
            var newClientOrderId = Guid.NewGuid().ToString("N");

            // Get datanase
            var database = Pool.DatabasePool.Get();

            // Get last orders
            var lastOrders = await database.GetLastOrdersAsync(this.Symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);
            var lastOrder = lastOrders.OrderByDescending(l => l.TransactionTime).FirstOrDefault();

            /*
             * Rules
             * Quote Order Qty Market orders have been enabled on all symbols.
             * Quote Order Qty MARKET orders allow a user to specify the total quoteOrderQty spent or received in the MARKET order.
             * Quote Order Qty MARKET orders will not break LOT_SIZE filter rules; the order will execute a quantity that will have the notional value as close as possible to quoteOrderQty.
             * 
             * Using BTCUSDT as an example:
             * On the BUY side, the order will buy as many BTC as quoteOrderQty USDT can.
             * On the SELL side, the order will sell as much BTC as needed to receive quoteOrderQty USDT.
             * 
            */

            WebCallResult<BinancePlacedOrder> placedOrder = null;
            var binanceClient = Program.Services.GetService(typeof(IBinanceClient)) as IBinanceClient;

            // Buy
            if (recommendation.Action == Analyze.Action.Buy)
            {
                this._logger.Verbose($"{this.Symbol}: Preparing to buy");

                // Get remaining USDT balance for trading
                var usdt = await this._accountant.GetBalance("USDT").ConfigureAwait(false);
                if (null == usdt)
                {
                    // Something is oddly wrong, wait a bit
                    this._logger.Debug("Cannot retrieve account info");
                    return;
                }

                // Only take half of what is available to buy assets
                var availableAmount = usdt?.Free * 0.5M;

                // Get ask price
                var bestAskPrice = this._buffer.GetAskPrice(this.Symbol);

                this._logger.Debug($"{this.Symbol}: {recommendation.Action} -bestAskPrice:{bestAskPrice};availableAmount:{availableAmount}");

                // Check if no order has been issued yet or order was SELL
                if ((null == lastOrder
                    || lastOrder.Side == OrderSide.Sell
                    || lastOrder.Side == OrderSide.Buy && lastOrder.Price * 0.99M > bestAskPrice && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow)
                    && availableAmount.HasValue && availableAmount.Value >= minNotional
                    && bestAskPrice > 0)
                {
                    this._logger.Debug($"{this.Symbol}: Issuing order to buy");
                    this._logger.Debug($"symbol:{this.Symbol};bestAskPrice:{bestAskPrice};quantity:{availableAmount}");

                    availableAmount = Math.Round(availableAmount.Value, exchangeInfo.BaseAssetPrecision, MidpointRounding.ToZero);

                    placedOrder = await binanceClient.PlaceOrderAsync(this.Symbol, OrderSide.Buy, OrderType.Market,
                        quoteOrderQuantity: availableAmount.Value, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                        ct: this._cancellationTokenSource.Token).ConfigureAwait(false);

                    await database.InsertAsync(new Order()
                    {
                        Symbol = this.Symbol,
                        Side = OrderSide.Buy,
                        Type = OrderType.Limit,
                        QuoteOrderQuantity = availableAmount.Value,
                        Price = bestAskPrice,
                        NewClientOrderId = newClientOrderId,
                        OrderResponseType = OrderResponseType.Full,
                        TimeInForce = TimeInForce.ImmediateOrCancel
                    }, this._cancellationTokenSource.Token).ConfigureAwait(false);

                    this._logger.Debug($"{this.Symbol}: Issued order to buy");
                }
            }
            // Sell
            else if (recommendation.Action == Analyze.Action.Sell)
            {
                this._logger.Debug($"{this.Symbol}: Preparing to sell");

                var assetBalance = await this._accountant.GetBalance(this.Symbol.Replace("USDT", string.Empty)).ConfigureAwait(false);
                var bestBidPrice = this._buffer.GetBidPrice(this.Symbol);
                // Sell 66% of the asset
                var assetBalanceForSale = assetBalance?.Free;
                var sellQuoteOrderQuantity = assetBalanceForSale.HasValue ? assetBalanceForSale * 0.5M * bestBidPrice : null;

                // Do not sell below buying price
                // Select trades where we bought
                // And where buying price is smaller than selling price
                // And where asset is available
                var availableQuantity = lastOrders.Where(l => l.Side == OrderSide.Buy && l.Price < (bestBidPrice * 0.999M) /*0.1% less*/ && l.Quantity > l.Consumed)
                    .Sum(l => (l.Quantity - l.Consumed));

                // Sell what is maximal possible (max or what was bought for less than it will be sold)
                sellQuoteOrderQuantity = sellQuoteOrderQuantity < availableQuantity ? sellQuoteOrderQuantity : availableQuantity;

                this._logger.Debug($"{this.Symbol}: {recommendation.Action} - bestBidPrice:{bestBidPrice};assetBalanceForSale:{assetBalanceForSale};sellQuoteOrderQuantity:{sellQuoteOrderQuantity}");

                // Check if no order has been issued yet or order was BUY
                if ((null == lastOrder
                    || lastOrder.Side == OrderSide.Buy
                    || lastOrder.Side == OrderSide.Sell && lastOrder.Price * 1.01M < bestBidPrice && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow)
                    && sellQuoteOrderQuantity.HasValue && sellQuoteOrderQuantity > 0 && sellQuoteOrderQuantity >= minNotional
                    /* implicit checking bestAskPrice > 0 by checking sellQuoteOrderQuantity > 0*/)
                {
                    this._logger.Debug($"{this.Symbol}: Issuing order to sell");

                    sellQuoteOrderQuantity = Math.Round(sellQuoteOrderQuantity.Value, exchangeInfo.BaseAssetPrecision, MidpointRounding.ToZero);

                    placedOrder = await binanceClient.PlaceOrderAsync(this.Symbol, OrderSide.Sell, OrderType.Market,
                        quoteOrderQuantity: sellQuoteOrderQuantity.Value, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                        ct: this._cancellationTokenSource.Token).ConfigureAwait(false);

                    await database.InsertAsync(new Order()
                    {
                        Symbol = this.Symbol,
                        Side = OrderSide.Sell,
                        Type = OrderType.Limit,
                        QuoteOrderQuantity = sellQuoteOrderQuantity.Value,
                        Price = bestBidPrice,
                        NewClientOrderId = newClientOrderId,
                        OrderResponseType = OrderResponseType.Full,
                        TimeInForce = TimeInForce.ImmediateOrCancel
                    }, this._cancellationTokenSource.Token).ConfigureAwait(false);

                    this._logger.Debug($"{this.Symbol}: Issued order to sell");
                }
            }

            // Check if order placed
            if (null != placedOrder)
            {
                // Check if order is OK and log
                if (placedOrder.ResponseStatusCode != System.Net.HttpStatusCode.OK || !placedOrder.Success)
                {
                    this._logger.Error($"Order {newClientOrderId} was malformed.");
                    this._logger.Error(placedOrder.Error?.Code.ToString());
                    this._logger.Error(placedOrder.Error?.Message);
                    this._logger.Error(placedOrder.Error?.Data?.ToString());
                }
                else
                {
                    await database.InsertAsync(placedOrder.Data, this._cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }

            Pool.DatabasePool.Put(database);
        }

        #endregion

        private BinanceSymbol _getExchangeInfo()
        {
            return this._buffer.GetExchangeInfoFor(this.Symbol);
        }

        #region Start / Stop

        public void Start(string symbolToTrade)
        {
            if (this._timerTrading.Enabled)
            {
                this._logger.Warning($"Trader for {this.Symbol} is already active.");
                return;
            }

            this._logger.Information("Starting Trader");

            if (this._buffer.GetSymbols().Contains(symbolToTrade))
            {
                this.Symbol = symbolToTrade;
            }

            this._timerTrading.Start();

            this._logger.Information("Trader started");
        }

        public async Task Stop()
        {
            if (!this._timerTrading.Enabled)
            {
                this._logger.Warning("Trader for {this.Symbol} is not active.");
                return;
            }

            this._logger.Information("Stopping Trader");

            this._timerTrading.Stop();

            // Give time for running task to end
            await Task.Delay(1000).ConfigureAwait(false);

            this._cancellationTokenSource.Cancel();

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
