using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Objects;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.DataLayer.Models;

namespace trape.cli.trader.Trading
{
    /// <summary>
    /// Does the actual trading taking into account the <c>Recommendation</c> of the <c>Analyst</c> and previous trades
    /// </summary>
    public class Broker : IBroker
    {
        #region Fields

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Accountant
        /// </summary>
        private readonly IAccountant _accountant;

        /// <summary>
        /// Analyst
        /// </summary>
        private readonly IAnalyst _analyst;

        /// <summary>
        /// Buffer
        /// </summary>
        private readonly IBuffer _buffer;

        /// <summary>
        /// Timer to check if sell/buy is recommended
        /// </summary>
        private System.Timers.Timer _timerTrading;

        /// <summary>
        /// Minimum required increase
        /// </summary>
        private const decimal minIncreaseRequired = 1.002M;

        /// <summary>
        /// Minimum required decrease
        /// </summary>
        private const decimal minDecreaseRequired = 0.998M;

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Records last strongs to not execute a strong <c>Recommendation</c> one after the other without a break inbetween
        /// </summary>
        private readonly Dictionary<Analyze.Action, DateTime> _lastRecommendation;

        /// <summary>
        /// Semaphore synchronizes access in case a task takes longer to return and the timer would elapse again
        /// </summary>
        private readonly SemaphoreSlim _canTrade;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Broker</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="analyst">Analyst</param>
        /// <param name="buffer">Buffer</param>
        public Broker(ILogger logger, IAccountant accountant, IAnalyst analyst, IBuffer buffer)
        {
            if (null == logger || null == accountant || null == analyst || null == buffer)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger.ForContext<Broker>();
            this._accountant = accountant;
            this._analyst = analyst;
            this._buffer = buffer;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._lastRecommendation = new Dictionary<Analyze.Action, DateTime>();
            this.Symbol = null;
            this._canTrade = new SemaphoreSlim(1, 1);

            // Set up timer
            this._timerTrading = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 0, 0, 100).TotalMilliseconds
            };
            this._timerTrading.Elapsed += _timerTrading_Elapsed;
        }

        #endregion

        #region Timer Elapsed

        /// <summary>
        /// Checks the <c>Recommendation</c> of the <c>Analyst</c> and previous trades and decides whether to buy, wait or sell
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            var recommendation = this._analyst.GetRecommendation(this.Symbol);
            if (null == recommendation || recommendation.Action == Analyze.Action.Wait)
            {
                this._logger.Verbose($"{this.Symbol}: Waiting for recommendation");
                return;
            }

            // If this point is reached, the preconditions are fine, check if slot is available
            if (this._canTrade.CurrentCount == 0)
            {
                return;
            }

            // Get database
            var database = Pool.DatabasePool.Get();
            try
            {
                // wait synchronizes because context is available, otherwise would have exited before reaching this step
                this._canTrade.Wait();

                // Get min notional
                var exchangeInfo = this._getExchangeInfo();

                // Generate new client order id
                var newClientOrderId = Guid.NewGuid().ToString("N");


                // Get last orders
                var lastOrders = await database.GetLastOrdersAsync(this.Symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);
                var lastOrder = lastOrders.Where(l => l.Symbol == this.Symbol).OrderByDescending(l => l.TransactionTime).FirstOrDefault();
                var lastOrderWithin31Minutes = lastOrders.Where(l => l.Symbol == this.Symbol && l.Side == lastOrder.Side);

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
                if (recommendation.Action == Analyze.Action.Buy || recommendation.Action == Analyze.Action.StrongBuy)
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
                    var availableUSDT = usdt?.Free * 0.6M;

                    // Round to a valid value
                    availableUSDT = Math.Round(availableUSDT.Value, exchangeInfo.BaseAssetPrecision, MidpointRounding.ToZero);

                    if (availableUSDT == 0)
                    {
                        this._logger.Verbose($"{this.Symbol} nothing free");
                    }
                    else
                    {
                        // Get ask price
                        var bestAskPrice = this._buffer.GetAskPrice(this.Symbol);

                        // Logging
                        this._logger.Verbose($"{this.Symbol}: {recommendation.Action} bestAskPrice:{Math.Round(bestAskPrice, exchangeInfo.BaseAssetPrecision)};availableAmount:{availableUSDT}");
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
                            || lastOrder.Side == OrderSide.Buy && lastOrder.Price * minDecreaseRequired > bestAskPrice && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow
                            || (!this._lastRecommendation.ContainsKey(recommendation.Action) || this._lastRecommendation[recommendation.Action].AddMinutes(1) < DateTime.UtcNow) && recommendation.Action == Analyze.Action.StrongBuy)
                            && availableUSDT.HasValue && availableUSDT.Value >= exchangeInfo.MinNotionalFilter.MinNotional
                            && bestAskPrice > 0)
                        {
                            this._logger.Debug($"{this.Symbol}: Issuing order to buy");
                            this._logger.Debug($"symbol:{this.Symbol};bestAskPrice:{bestAskPrice};quantity:{availableUSDT}");

                            // Place the order
                            placedOrder = await binanceClient.PlaceOrderAsync(this.Symbol, OrderSide.Buy, OrderType.Market,
                                quoteOrderQuantity: availableUSDT.Value, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                                ct: this._cancellationTokenSource.Token).ConfigureAwait(true);

                            // Log order in custom format
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
                }
                // Sell
                else if (recommendation.Action == Analyze.Action.Sell || recommendation.Action == Analyze.Action.StrongSell || recommendation.Action == Analyze.Action.PanicSell)
                {
                    this._logger.Debug($"{this.Symbol}: Preparing to sell");

                    var assetBalance = await this._accountant.GetBalance(this.Symbol.Replace("USDT", string.Empty)).ConfigureAwait(false);

                    if (assetBalance == null || assetBalance?.Free == 0)
                    {
                        this._logger.Verbose($"{this.Symbol} nothing free");
                    }
                    else
                    {
                        var assetBalanceFree = assetBalance?.Free;
                        // Sell 85% of the asset
                        var assetBalanceToSell = assetBalanceFree.HasValue ? assetBalanceFree * 0.85M : null;

                        var bestBidPrice = this._buffer.GetBidPrice(this.Symbol);
                        // Do not sell below buying price
                        // Select trades where we bought
                        // And where buying price is smaller than selling price
                        // And where asset is available
                        var availableAssetQuantity = lastOrders.Where(l => l.Side == OrderSide.Buy && l.Price < (bestBidPrice * 0.999M) /*0.001% less*/ && l.Quantity > l.Consumed)
                            .Sum(l => (l.Quantity - l.Consumed));

                        // Sell what is maximal possible (max or what was bought for less than it will be sold), because of rounding and commission reduct 1% from availableQuantity
                        assetBalanceToSell = assetBalanceToSell < (availableAssetQuantity * 0.99M) ? assetBalanceToSell : (availableAssetQuantity * 0.99M);

                        // Panic Mode
                        if (recommendation.Action == Analyze.Action.PanicSell)
                        {
                            assetBalanceFree = assetBalance?.Free;
                            bestBidPrice = bestBidPrice * 0.999M; // Reduce by 0.1 percent to definitely sell
                        }

                        // Sell as much required to get this total USDT price
                        var aimToGetUSDT = assetBalanceToSell * bestBidPrice;
                        // Round to a valid value
                        aimToGetUSDT = Math.Round(aimToGetUSDT.Value, exchangeInfo.BaseAssetPrecision, MidpointRounding.ToZero);

                        // Logging
                        this._logger.Verbose($"{this.Symbol}: {recommendation.Action} bestBidPrice:{Math.Round(bestBidPrice, exchangeInfo.BaseAssetPrecision)};assetBalanceForSale:{assetBalanceFree};sellQuoteOrderQuantity:{assetBalanceToSell};sellToGetUSDT:{aimToGetUSDT}");
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
                            || lastOrder.Side == OrderSide.Sell && lastOrder.Price * minIncreaseRequired < bestBidPrice && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow
                            || (!this._lastRecommendation.ContainsKey(recommendation.Action) || this._lastRecommendation[recommendation.Action].AddMinutes(1) < DateTime.UtcNow) && recommendation.Action == Analyze.Action.StrongSell)
                            || recommendation.Action == Analyze.Action.PanicSell
                            && aimToGetUSDT.HasValue && aimToGetUSDT > exchangeInfo.MinNotionalFilter.MinNotional
                            /* implicit checking bestAskPrice > 0 by checking sellQuoteOrderQuantity > 0*/)
                        {
                            this._logger.Debug($"{this.Symbol}: Issuing order to sell");

                            if (recommendation.Action == Analyze.Action.PanicSell)
                            {
                                this._logger.Warning($"{this.Symbol}: PANICKING");
                            }

                            // Place the order
                            placedOrder = await binanceClient.PlaceOrderAsync(this.Symbol, OrderSide.Sell, OrderType.Market,
                                quoteOrderQuantity: aimToGetUSDT.Value, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                                ct: this._cancellationTokenSource.Token).ConfigureAwait(true);

                            // Log order in custom format
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
                }

                // Log recommended action, use for strong recommendations
                if (this._lastRecommendation.ContainsKey(recommendation.Action))
                {
                    this._lastRecommendation[recommendation.Action] = DateTime.UtcNow;
                }
                else
                {
                    this._lastRecommendation.Add(recommendation.Action, DateTime.UtcNow);
                }

                // Check if order placed
                if (null != placedOrder)
                {
                    // Check if order is OK and log
                    if (placedOrder.ResponseStatusCode != System.Net.HttpStatusCode.OK || !placedOrder.Success)
                    {
                        using (var context = LogContext.PushProperty("placedOrder", placedOrder))
                        {
                            this._logger.Error($"Order {newClientOrderId} was malformed");
                        }

                        // Logging
                        this._logger.Error(placedOrder.Error?.Code.ToString());
                        this._logger.Error(placedOrder.Error?.Message);
                        this._logger.Error(placedOrder.Error?.Data?.ToString());
                        this._logger.Warning($"PlacedOrder: {placedOrder.Data.Symbol};{placedOrder.Data.Side};{placedOrder.Data.Type} > ClientOrderId:{placedOrder.Data.ClientOrderId} CummulativeQuoteQuantity:{placedOrder.Data.CummulativeQuoteQuantity} OriginalQuoteOrderQuantity:{placedOrder.Data.OriginalQuoteOrderQuantity} Status:{placedOrder.Data.Status}");
                    }
                    else
                    {
                        await database.InsertAsync(placedOrder.Data, this._cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }                
            }
            /// Catch All Exception
            catch(Exception cae)
            {
                this._logger.Error(cae, cae.Message);
            }
            finally
            {
                // Return database instance
                Pool.DatabasePool.Put(database);

                // Release lock
                this._canTrade.Release();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets latest exchange information
        /// </summary>
        /// <returns></returns>
        private BinanceSymbol _getExchangeInfo()
        {
            return this._buffer.GetExchangeInfoFor(this.Symbol);
        }

        #endregion

        #region Start / Stop

        public void Start(string symbolToTrade)
        {
            if (this._timerTrading.Enabled)
            {
                this._logger.Warning($"Trader for {symbolToTrade} is already active");
                return;
            }

            this._logger.Information($"Starting Trader for {symbolToTrade}");

            if (this._buffer.GetSymbols().Contains(symbolToTrade))
            {
                this.Symbol = symbolToTrade;

                this._timerTrading.Start();

                this._logger.Information($"Trader for {this.Symbol} started");
            }
            else
            {
                this._logger.Error($"Trader for {symbolToTrade} cannot be started, symbol does not exist");
            }
        }

        public async Task Finish()
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
