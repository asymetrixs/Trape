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

        private IAnalyst _recommender;

        private IBuffer _buffer;

        private System.Timers.Timer _timerTrading;

        private const decimal minIncreaseRequired = 1.002M;

        private const decimal minDecreaseRequired = 0.998M;

        public string Symbol { get; private set; }

        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Constructor

        public Trader(ILogger logger, IAccountant accountant, IAnalyst recommender, IBuffer buffer)
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

            // Generate new client order id
            var newClientOrderId = Guid.NewGuid().ToString("N");

            // Get database
            var database = Pool.DatabasePool.Get();

            // Get last orders
            var lastOrders = await database.GetLastOrdersAsync(this.Symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);
            var lastOrder = lastOrders.Where(l => l.Symbol == this.Symbol).OrderByDescending(l => l.TransactionTime).FirstOrDefault();

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
                var availableUSDT = usdt?.Free * 0.5M;
                // Round to a valid value
                availableUSDT = Math.Round(availableUSDT.Value, exchangeInfo.BaseAssetPrecision, MidpointRounding.ToZero);

                // Get ask price
                var bestAskPrice = this._buffer.GetAskPrice(this.Symbol);

                this._logger.Debug($"{this.Symbol}: {recommendation.Action} bestAskPrice:{bestAskPrice};availableAmount:{availableUSDT}");
                this._logger.Verbose($"{this.Symbol} Buy : Checking conditions");
                this._logger.Verbose($"{this.Symbol} Buy : lastOrder is null: {null == lastOrder}");
                this._logger.Verbose($"{this.Symbol} Buy : lastOrder side: {lastOrder?.Side.ToString()}");
                this._logger.Verbose($"{this.Symbol} Buy : lastOrder Price: {lastOrder?.Price} * {minDecreaseRequired} > {bestAskPrice}: {lastOrder?.Price * minDecreaseRequired > bestAskPrice}");
                this._logger.Verbose($"{this.Symbol} Buy : Transaction Time: {lastOrder?.TransactionTime} + 15 minutes ({lastOrder?.TransactionTime.AddMinutes(15)}) < {DateTime.UtcNow}: {lastOrder?.TransactionTime.AddMinutes(15) < DateTime.UtcNow}");
                this._logger.Verbose($"{this.Symbol} Buy : availableAmount has value {availableUSDT.HasValue}");
                if (availableUSDT.HasValue)
                {
                    this._logger.Verbose($"Value {availableUSDT} > 0: {availableUSDT > 0} and higher than minNotional {exchangeInfo.MinNotionalFilter.MinNotional}: {availableUSDT >= exchangeInfo.MinNotionalFilter.MinNotional}");
                }

                // Check if no order has been issued yet or order was SELL
                if ((null == lastOrder
                    || lastOrder.Side == OrderSide.Sell
                    || lastOrder.Side == OrderSide.Buy && lastOrder.Price * minDecreaseRequired > bestAskPrice && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow)
                    && availableUSDT.HasValue && availableUSDT.Value >= exchangeInfo.MinNotionalFilter.MinNotional
                    && bestAskPrice > 0)
                {
                    this._logger.Debug($"{this.Symbol}: Issuing order to buy");
                    this._logger.Debug($"symbol:{this.Symbol};bestAskPrice:{bestAskPrice};quantity:{availableUSDT}");

                    placedOrder = await binanceClient.PlaceOrderAsync(this.Symbol, OrderSide.Buy, OrderType.Market,
                        quoteOrderQuantity: availableUSDT.Value, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                        ct: this._cancellationTokenSource.Token).ConfigureAwait(true);

                    await database.InsertAsync(new Order()
                    {
                        Symbol = this.Symbol,
                        Side = OrderSide.Buy,
                        Type = OrderType.Limit,
                        QuoteOrderQuantity = availableUSDT.Value,
                        Price = bestAskPrice,
                        NewClientOrderId = newClientOrderId,
                        OrderResponseType = OrderResponseType.Full,
                        TimeInForce = TimeInForce.ImmediateOrCancel
                    }, this._cancellationTokenSource.Token).ConfigureAwait(true);

                    this._logger.Debug($"{this.Symbol}: Issued order to buy");
                }
                else
                {
                    this._logger.Debug($"{this.Symbol}: Skipping, final conditions not met");
                }
            }
            // Sell
            else if (recommendation.Action == Analyze.Action.Sell)
            {
                this._logger.Debug($"{this.Symbol}: Preparing to sell");

                var assetBalance = await this._accountant.GetBalance(this.Symbol.Replace("USDT", string.Empty)).ConfigureAwait(false);
                var bestBidPrice = this._buffer.GetBidPrice(this.Symbol);
                // Sell 70% of the asset
                var assetBalanceFree = assetBalance?.Free;
                var assetBalanceToSell = assetBalanceFree.HasValue ? assetBalanceFree * 0.70M : null;

                // Do not sell below buying price
                // Select trades where we bought
                // And where buying price is smaller than selling price
                // And where asset is available
                var availableAssetQuantity = lastOrders.Where(l => l.Side == OrderSide.Buy && l.Price < (bestBidPrice * 0.999M) /*0.001% less*/ && l.Quantity > l.Consumed)
                    .Sum(l => (l.Quantity - l.Consumed));

                // Sell what is maximal possible (max or what was bought for less than it will be sold), because of rounding and commission reduct 1% from availableQuantity
                assetBalanceToSell = assetBalanceToSell < (availableAssetQuantity * 0.99M) ? assetBalanceToSell : (availableAssetQuantity * 0.99M);

                // Sell as much required to get this total USDT price
                var aimToGetUSDT = assetBalanceToSell * bestBidPrice;
                // Round to a valid value
                aimToGetUSDT = Math.Round(aimToGetUSDT.Value, exchangeInfo.BaseAssetPrecision, MidpointRounding.ToZero);

                this._logger.Debug($"{this.Symbol}: {recommendation.Action} bestBidPrice:{bestBidPrice};assetBalanceForSale:{assetBalanceFree};sellQuoteOrderQuantity:{assetBalanceToSell};sellToGetUSDT:{aimToGetUSDT}");
                this._logger.Verbose($"{this.Symbol} Sell: Checking conditions");
                this._logger.Verbose($"{this.Symbol} Sell: lastOrder is null: {null == lastOrder}");
                this._logger.Verbose($"{this.Symbol} Sell: lastOrder side: {lastOrder?.Side.ToString()}");
                this._logger.Verbose($"{this.Symbol} Sell: lastOrder Price: {lastOrder?.Price} * {minIncreaseRequired} < {bestBidPrice}: {lastOrder?.Price * minIncreaseRequired < bestBidPrice}");
                this._logger.Verbose($"{this.Symbol} Sell: Transaction Time: {lastOrder?.TransactionTime} + 15 minutes ({lastOrder?.TransactionTime.AddMinutes(15)}) < {DateTime.UtcNow}: {lastOrder?.TransactionTime.AddMinutes(15) < DateTime.UtcNow}");
                this._logger.Verbose($"{this.Symbol} Sell: aimToGetUSDT has value {aimToGetUSDT.HasValue} -> {aimToGetUSDT.Value}");
                if (aimToGetUSDT.HasValue)
                {
                    this._logger.Verbose($"Value {aimToGetUSDT} > 0: {aimToGetUSDT > 0} and higher than {exchangeInfo.MinNotionalFilter.MinNotional}: {aimToGetUSDT >= exchangeInfo.MinNotionalFilter.MinNotional}");
                }

                this._logger.Verbose($"Sell X to get {aimToGetUSDT}");


                // Check if no order has been issued yet or order was BUY
                if ((null == lastOrder
                    || lastOrder.Side == OrderSide.Buy
                    || lastOrder.Side == OrderSide.Sell && lastOrder.Price * minIncreaseRequired < bestBidPrice && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow)
                    && aimToGetUSDT.HasValue && aimToGetUSDT > exchangeInfo.MinNotionalFilter.MinNotional
                    /* implicit checking bestAskPrice > 0 by checking sellQuoteOrderQuantity > 0*/)
                {
                    this._logger.Debug($"{this.Symbol}: Issuing order to sell");

                    placedOrder = await binanceClient.PlaceOrderAsync(this.Symbol, OrderSide.Sell, OrderType.Market,
                        quoteOrderQuantity: aimToGetUSDT.Value, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                        ct: this._cancellationTokenSource.Token).ConfigureAwait(true);

                    await database.InsertAsync(new Order()
                    {
                        Symbol = this.Symbol,
                        Side = OrderSide.Sell,
                        Type = OrderType.Limit,
                        QuoteOrderQuantity = aimToGetUSDT.Value,
                        Price = bestBidPrice,
                        NewClientOrderId = newClientOrderId,
                        OrderResponseType = OrderResponseType.Full,
                        TimeInForce = TimeInForce.ImmediateOrCancel
                    }, this._cancellationTokenSource.Token).ConfigureAwait(true);

                    this._logger.Debug($"{this.Symbol}: Issued order to sell");
                }
                else
                {
                    this._logger.Debug($"{this.Symbol}: Skipping, final conditions not met");
                }
            }

            // Check if order placed
            if (null != placedOrder)
            {
                // Check if order is OK and log
                if (placedOrder.ResponseStatusCode != System.Net.HttpStatusCode.OK || !placedOrder.Success)
                {
                    this._logger.Error($"Order {newClientOrderId} was malformed");
                    this._logger.Error(placedOrder.Error?.Code.ToString());
                    this._logger.Error(placedOrder.Error?.Message);
                    this._logger.Error(placedOrder.Error?.Data?.ToString());
                }
                else
                {
                    await database.InsertAsync(placedOrder.Data, this._cancellationTokenSource.Token).ConfigureAwait(false);

                    try
                    {
                        if (placedOrder.Data.Side == OrderSide.Sell)
                        {
                            Console.Beep(1000, 500);
                            await Task.Delay(200).ConfigureAwait(true);
                            Console.Beep(1000, 500);
                            await Task.Delay(200).ConfigureAwait(true);
                            Console.Beep(1000, 500);
                        }
                        else
                        {
                            Console.Beep(1000, 1500);
                        }
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // nothing
                    }
                    catch (ArgumentOutOfRangeException aofre)
                    {
                        this._logger.Error(aofre.Message, aofre);
                        throw;
                    }
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
                this._logger.Warning($"Trader for {this.Symbol} is already active");
                return;
            }

            this._logger.Information($"Starting Trader for {this.Symbol}");

            if (this._buffer.GetSymbols().Contains(symbolToTrade))
            {
                this.Symbol = symbolToTrade;
            }

            this._timerTrading.Start();

            this._logger.Information($"Trader for {this.Symbol} started");
        }

        public async Task Stop()
        {
            if (!this._timerTrading.Enabled)
            {
                this._logger.Warning($"Trader for {this.Symbol} is not active");
                return;
            }

            this._logger.Information($"Stopping Trader for {this.Symbol}");

            this._timerTrading.Stop();

            // Give time for running task to end
            await Task.Delay(1000).ConfigureAwait(false);

            this._cancellationTokenSource.Cancel();

            this._logger.Information($"Trader for {this.Symbol} stopped");
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
