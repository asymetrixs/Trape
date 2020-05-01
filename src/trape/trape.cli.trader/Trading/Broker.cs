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
using trape.cli.trader.DataLayer;
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
        /// Minimum required increase of the price before another chunk is sold
        /// </summary>
        private const decimal requiredPriceGainForResell = 1.003M;

        /// <summary>
        /// Minimum required decrease of the price before another chunk is bought
        /// </summary>
        private const decimal requiredPriceDropforRebuy = 0.997M;

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

        /// <summary>
        /// Binance Client
        /// </summary>
        private readonly IBinanceClient _binanceClient;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Broker</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="analyst">Analyst</param>
        /// <param name="buffer">Buffer</param>
        public Broker(ILogger logger, IAccountant accountant, IAnalyst analyst, IBuffer buffer, IBinanceClient binanceClient)
        {
            if (logger == null || accountant == null || analyst == null || buffer == null || binanceClient == null)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger.ForContext<Broker>();
            this._accountant = accountant;
            this._analyst = analyst;
            this._buffer = buffer;
            this._binanceClient = binanceClient;
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

            // Get recommendation
            var recommendation = this._analyst.GetRecommendation(this.Symbol);
            if (null == recommendation)
            {
                this._logger.Verbose($"{this.Symbol}: No recommendation available.");
                return;
            }
            else if (recommendation.Action == Analyze.Action.Hold)
            {
                this._logger.Verbose($"{this.Symbol}: Recommendation is hold.");
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
                // Wait because context is available, otherwise would have exited before reaching this step
                // Synchronizing is just used in the rare case that a method needs longer to return than the timer needs to elapse
                // so that no other task runs the same method
                this._canTrade.Wait();

                // Get min notional
                var symbolInfo = this._getSymbolInfo();

                // If exchange information are not yet available, return
                if (symbolInfo == null)
                {
                    return;
                }

                // Generate new client order id
                var newClientOrderId = Guid.NewGuid().ToString("N");


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

                // Buy
                if (recommendation.Action == Analyze.Action.Buy
                    || recommendation.Action == Analyze.Action.StrongBuy)
                {
                    placedOrder = await this.Buy(recommendation, symbolInfo, database, newClientOrderId, lastOrder).ConfigureAwait(true);
                }
                // Sell
                else if (recommendation.Action == Analyze.Action.Sell
                    || recommendation.Action == Analyze.Action.StrongSell
                    || recommendation.Action == Analyze.Action.PanicSell
                    || recommendation.Action == Analyze.Action.TakeProfitsSell)
                {
                    placedOrder = await this.Sell(recommendation, lastOrders, symbolInfo, database, newClientOrderId, lastOrder).ConfigureAwait(true);
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

                await this.LogOrder(database, newClientOrderId, placedOrder).ConfigureAwait(false);
            }
            // Catch All Exception
            catch (Exception cae)
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

        #region Trading Methods

        /// <summary>
        /// Logs an order in the database
        /// </summary>
        /// <param name="database">Database</param>
        /// <param name="newClientOrderId">Generated new client order id</param>
        /// <param name="placedOrder">Placed order or null if none</param>
        /// <returns></returns>
        private async Task LogOrder(ITrapeContext database, string newClientOrderId, WebCallResult<BinancePlacedOrder> placedOrder = null)
        {
            #region Argument checks

            if (database == null)
                throw new ArgumentNullException(paramName: nameof(database));

            if (string.IsNullOrEmpty(newClientOrderId))
                throw new ArgumentNullException(paramName: nameof(newClientOrderId));

            #endregion

            // Check if order placed
            if (null != placedOrder)
            {
                // Check if order is OK and log
                if (placedOrder.ResponseStatusCode != System.Net.HttpStatusCode.OK || !placedOrder.Success)
                {
                    using (var context1 = LogContext.PushProperty("placedOrder.Error", placedOrder.Error))
                    using (var context2 = LogContext.PushProperty("placedOrder.Data", placedOrder.Data))
                    {
                        this._logger.Error($"Order {newClientOrderId} was malformed");
                    }

                    // Logging
                    this._logger.Error(placedOrder.Error?.Code.ToString());
                    this._logger.Error(placedOrder.Error?.Message);
                    this._logger.Error(placedOrder.Error?.Data?.ToString());

                    if (placedOrder.Data != null)
                    {
                        this._logger.Warning($"PlacedOrder: {placedOrder.Data.Symbol};{placedOrder.Data.Side};{placedOrder.Data.Type} > ClientOrderId:{placedOrder.Data.ClientOrderId} CummulativeQuoteQuantity:{placedOrder.Data.CummulativeQuoteQuantity} OriginalQuoteOrderQuantity:{placedOrder.Data.OriginalQuoteOrderQuantity} Status:{placedOrder.Data.Status}");
                    }
                }
                else
                {
                    await database.InsertAsync(placedOrder.Data, this._cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Runs some checks and places an order if possible
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <param name="symbolInfo">Symbol information</param>
        /// <param name="database">Database conneciton</param>
        /// <param name="newClientOrderId">Generated new client order id</param>
        /// <param name="lastOrder">Last order, if any</param>
        /// <returns>Placed order or null if none placed</returns>
        public async Task<WebCallResult<BinancePlacedOrder>> Buy(Recommendation recommendation, BinanceSymbol symbolInfo, ITrapeContext database, string newClientOrderId, LastOrder lastOrder = null)
        {
            #region Argument checks

            if (recommendation == null)
                throw new ArgumentNullException(paramName: nameof(recommendation));

            if (symbolInfo == null)
                throw new ArgumentNullException(paramName: nameof(symbolInfo));

            if (database == null)
                throw new ArgumentNullException(paramName: nameof(database));

            if (string.IsNullOrEmpty(newClientOrderId))
                throw new ArgumentNullException(paramName: nameof(newClientOrderId));

            #endregion

            WebCallResult<BinancePlacedOrder> placedOrder = null;

            this._logger.Debug($"{this.Symbol}: Preparing to buy");

            // Get remaining USDT balance for trading
            var usdt = await this._accountant.GetBalance("USDT").ConfigureAwait(false);
            if (null == usdt)
            {
                // Something is oddly wrong, wait a bit
                this._logger.Debug("Cannot retrieve account info");
                return placedOrder;
            }

            // Only take half of what is available to buy assets
            var availableUSDT = usdt?.Free * 0.6M;
            // In case of strong buy increase budget
            if (recommendation.Action == Analyze.Action.StrongBuy)
            {
                availableUSDT = usdt?.Free * 0.8M;
            }

            // Round to a valid value
            availableUSDT = Math.Round(availableUSDT.Value, symbolInfo.BaseAssetPrecision, MidpointRounding.ToZero);

            if (availableUSDT == 0)
            {
                this._logger.Debug($"{this.Symbol} nothing free");
            }
            else
            {
                // Get ask price
                var bestAskPrice = this._buffer.GetAskPrice(this.Symbol);

                // Logging
                this._logger.Debug($"{this.Symbol}: {recommendation.Action} bestAskPrice:{Math.Round(bestAskPrice, symbolInfo.BaseAssetPrecision)};availableAmount:{availableUSDT}");
                this._logger.Debug($"{this.Symbol} Buy : Checking conditions");
                this._logger.Debug($"{this.Symbol} Buy : {null == lastOrder} lastOrder is null");
                this._logger.Debug($"{this.Symbol} Buy : {lastOrder?.Side.ToString()} lastOrder side");
                this._logger.Debug($"{this.Symbol} Buy : {lastOrder?.Price * requiredPriceDropforRebuy > bestAskPrice} lastOrder Price: {lastOrder?.Price} * {requiredPriceDropforRebuy} > {bestAskPrice}");
                this._logger.Debug($"{this.Symbol} Buy : {lastOrder?.TransactionTime.AddMinutes(15) < DateTime.UtcNow} Transaction Time: {lastOrder?.TransactionTime} + 15 minutes ({lastOrder?.TransactionTime.AddMinutes(5)}) < {DateTime.UtcNow}");
                this._logger.Debug($"{this.Symbol} Buy : {availableUSDT.HasValue} availableAmount has value: {availableUSDT.Value}");
                this._logger.Debug($"{this.Symbol} Buy : {recommendation.Action} recommendation");
                if (availableUSDT.HasValue)
                {
                    this._logger.Debug($"{this.Symbol} Buy : {availableUSDT.Value >= symbolInfo.MinNotionalFilter.MinNotional} Value {availableUSDT.Value} > 0: {availableUSDT.Value > 0} and higher than minNotional {symbolInfo.MinNotionalFilter.MinNotional}");
                    this._logger.Debug($"{this.Symbol} Buy : {symbolInfo.PriceFilter.MaxPrice >= availableUSDT.Value && availableUSDT.Value >= symbolInfo.PriceFilter.MinPrice} MaxPrice {symbolInfo.PriceFilter.MaxPrice} >= Amount {availableUSDT.Value} >= MinPrice {symbolInfo.PriceFilter.MinPrice}");
                    this._logger.Debug($"{this.Symbol} Buy : {symbolInfo.LotSizeFilter.MaxQuantity >= availableUSDT / bestAskPrice && availableUSDT / bestAskPrice >= symbolInfo.LotSizeFilter.MinQuantity} MaxLOT {symbolInfo.LotSizeFilter.MaxQuantity} > Amount {availableUSDT / bestAskPrice} > MinLOT {symbolInfo.LotSizeFilter.MinQuantity}");
                }

                // For increased readability
                var isLastOrderNull = lastOrder == null;
                var isLastOrderSell = !isLastOrderNull && lastOrder.Side == OrderSide.Sell;
                var isLastOrderBuyAndPriceDecreased = !isLastOrderNull && lastOrder.Side == OrderSide.Buy
                                                        && lastOrder.Price * requiredPriceDropforRebuy > bestAskPrice
                                                        && lastOrder.TransactionTime.AddMinutes(5) < DateTime.UtcNow;
                var shallFollowStrongBuy = (!this._lastRecommendation.ContainsKey(recommendation.Action)
                                                            || this._lastRecommendation[recommendation.Action].AddMinutes(1) < DateTime.UtcNow)
                                                        && recommendation.Action == Analyze.Action.StrongBuy;

                var isLogicValid = availableUSDT.HasValue
                                    && availableUSDT.Value >= symbolInfo.MinNotionalFilter.MinNotional
                                    && bestAskPrice > 0;

                var isPriceRangeValid = availableUSDT.Value >= symbolInfo.PriceFilter.MinPrice
                                            && availableUSDT.Value <= symbolInfo.PriceFilter.MaxPrice;

                var isLOTSizeValid = availableUSDT / bestAskPrice >= symbolInfo.LotSizeFilter.MinQuantity
                                        && availableUSDT / bestAskPrice <= symbolInfo.LotSizeFilter.MaxQuantity;

                this._logger.Debug($"{this.Symbol} Buy : {isLastOrderNull} isLastOrderNull");
                this._logger.Debug($"{this.Symbol} Buy : {isLastOrderSell} isLastOrderSell");
                this._logger.Debug($"{this.Symbol} Buy : {isLastOrderBuyAndPriceDecreased} isLastOrderBuyAndPriceDecreased");
                this._logger.Debug($"{this.Symbol} Buy : {shallFollowStrongBuy} shallFollowStrongBuy");
                this._logger.Debug($"{this.Symbol} Buy : {isLogicValid} isLogicValid");
                this._logger.Debug($"{this.Symbol} Buy : {isPriceRangeValid} isPriceRangeValid");
                this._logger.Debug($"{this.Symbol} Buy : {isLOTSizeValid} isLOTSizeValid");

                // Check conditions for buy
                if (
                        (
                        isLastOrderNull
                        || isLastOrderSell
                        || isLastOrderBuyAndPriceDecreased
                        || shallFollowStrongBuy
                        )
                        // Logic check
                        && isLogicValid
                        // Price range check
                        && isPriceRangeValid
                        // LOT size check
                        && isLOTSizeValid
                    )
                {
                    this._logger.Debug($"{this.Symbol}: Issuing order to buy");
                    this._logger.Debug($"symbol:{this.Symbol};bestAskPrice:{bestAskPrice};quantity:{availableUSDT}");

                    // Place the order
                    placedOrder = await this._binanceClient.PlaceOrderAsync(this.Symbol, OrderSide.Buy, OrderType.Market,
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

            return placedOrder;
        }

        /// <summary>
        /// Runs some checks and places an order if possible
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <param name="lastOrders">Last orders</param>
        /// <param name="symbolInfo">Symbol info</param>
        /// <param name="database">Database connection</param>
        /// <param name="newClientOrderId">Generated new client order id</param>
        /// <param name="lastOrder">Last order, if any</param>
        /// <returns>Placed order or null if none placed</returns>
        public async Task<WebCallResult<BinancePlacedOrder>> Sell(Recommendation recommendation, IEnumerable<LastOrder> lastOrders, BinanceSymbol symbolInfo, ITrapeContext database, string newClientOrderId, LastOrder lastOrder = null)
        {
            #region Argument checks

            if (recommendation == null)
                throw new ArgumentNullException(paramName: nameof(recommendation));

            if (lastOrders == null)
                throw new ArgumentNullException(paramName: nameof(lastOrders));

            if (symbolInfo == null)
                throw new ArgumentNullException(paramName: nameof(symbolInfo));

            if (database == null)
                throw new ArgumentNullException(paramName: nameof(database));

            if (string.IsNullOrEmpty(newClientOrderId))
                throw new ArgumentNullException(paramName: nameof(newClientOrderId));

            #endregion

            WebCallResult<BinancePlacedOrder> placedOrder = null;

            this._logger.Debug($"{this.Symbol}: Preparing to sell");

            var assetBalance = await this._accountant.GetBalance(this.Symbol.Replace("USDT", string.Empty)).ConfigureAwait(false);

            if (assetBalance == null || assetBalance?.Free == 0)
            {
                this._logger.Debug($"{this.Symbol} nothing free");

                if (recommendation.Action == Analyze.Action.PanicSell)
                {
                    this._logger.Warning($"{this.Symbol}: PANICKING - but no asset free for sale");
                }
            }
            else
            {
                // Sell 85% of the asset
                var assetBalanceToSell = assetBalance?.Free * 0.85M;

                // Get best bid price
                var bestBidPrice = this._buffer.GetBidPrice(this.Symbol);

                // Normal - Do not sell below buying price
                // - Where buying price is smaller than selling price * 0.998
                // Stop-Loss - Limit loss
                // - Where buying-price was 0.5% higher than current price
                // And where assets are available and side is buy
                // Select quantities
                var availableAssetQuantity = lastOrders.Where(l => l.Side == OrderSide.Buy
                                                            && (
                                                                /* Normal: 0.2% lower */
                                                                (l.Price < (bestBidPrice * 0.998M) /*  */)
                                                                ||
                                                                /* Stop-Loss: 0.5% higher */
                                                                (l.Price > (bestBidPrice * 1.005M)
                                                                    && recommendation.Action == Analyze.Action.StrongSell)
                                                                )
                                                            && l.Quantity > l.Consumed)
                                                        .Sum(l => (l.Quantity - l.Consumed));

                // Sell what is maximal possible (from what was bought or (in case of discrepancy between recorded and actual value) what is possible
                assetBalanceToSell = assetBalanceToSell < (availableAssetQuantity * 0.99M) ? assetBalanceToSell : (availableAssetQuantity * 0.99M);

                // Panic Mode, sell everything
                if (recommendation.Action == Analyze.Action.PanicSell)
                {
                    assetBalanceToSell = assetBalance?.Free;
                }
                else if (recommendation.Action == Analyze.Action.StrongSell)
                {
                    // TODO: Sell everything on break-even
                    // Sell everything with profit or at least 75% of what is in stock
                    assetBalanceToSell = assetBalance?.Free * 0.75M < assetBalanceToSell ? assetBalanceToSell : assetBalance?.Free * 0.75M;
                }

                // Sell as much required to get this total USDT price
                var aimToGetUSDT = assetBalanceToSell * bestBidPrice;
                // Round to a valid value
                aimToGetUSDT = Math.Round(aimToGetUSDT.Value, symbolInfo.BaseAssetPrecision, MidpointRounding.ToZero);

                // Logging
                this._logger.Debug($"{this.Symbol}: {recommendation.Action} bestBidPrice:{Math.Round(bestBidPrice, symbolInfo.BaseAssetPrecision)};assetBalance.Free:{assetBalance?.Free};sellQuoteOrderQuantity:{assetBalanceToSell};sellToGetUSDT:{aimToGetUSDT}");
                this._logger.Debug($"{this.Symbol} Sell: Checking conditions");
                this._logger.Debug($"{this.Symbol} Sell: {null == lastOrder} lastOrder is null");
                this._logger.Debug($"{this.Symbol} Sell: {lastOrder?.Side.ToString()} lastOrder side");
                this._logger.Debug($"{this.Symbol} Sell: {lastOrder?.Price * requiredPriceGainForResell < bestBidPrice} lastOrder Price: {lastOrder?.Price} * {requiredPriceGainForResell} < {bestBidPrice}");
                this._logger.Debug($"{this.Symbol} Sell: {lastOrder?.TransactionTime.AddMinutes(15) < DateTime.UtcNow} Transaction Time: {lastOrder?.TransactionTime} + 15 minutes ({lastOrder?.TransactionTime.AddMinutes(5)}) < {DateTime.UtcNow}");
                this._logger.Debug($"{this.Symbol} Sell: {aimToGetUSDT.HasValue} aimToGetUSDT has value: {aimToGetUSDT.Value}");
                this._logger.Debug($"{this.Symbol} Sell: {recommendation.Action} recommendation");
                if (aimToGetUSDT.HasValue)
                {
                    this._logger.Debug($"{this.Symbol} Sell: {aimToGetUSDT >= symbolInfo.MinNotionalFilter.MinNotional} Value {aimToGetUSDT} > 0: {aimToGetUSDT > 0} and higher than {symbolInfo.MinNotionalFilter.MinNotional}");
                    this._logger.Debug($"{this.Symbol} Sell: {symbolInfo.PriceFilter.MaxPrice >= aimToGetUSDT.Value && aimToGetUSDT.Value >= symbolInfo.PriceFilter.MinPrice} MaxPrice { symbolInfo.PriceFilter.MaxPrice} > Amount {aimToGetUSDT.Value} > MinPrice {symbolInfo.PriceFilter.MinPrice}");
                    this._logger.Debug($"{this.Symbol} Sell: {symbolInfo.LotSizeFilter.MaxQuantity >= assetBalanceToSell && assetBalanceToSell >= symbolInfo.LotSizeFilter.MinQuantity} MaxLOT {symbolInfo.LotSizeFilter.MaxQuantity} > Amount {assetBalanceToSell} > MinLOT {symbolInfo.LotSizeFilter.MinQuantity}");
                }

                this._logger.Debug($"Sell X to get {aimToGetUSDT}");

                if (recommendation.Action == Analyze.Action.PanicSell)
                {
                    this._logger.Warning($"{this.Symbol}: PANICKING - preparing sell of {assetBalanceToSell} for {aimToGetUSDT.Value}");
                }
                else if (recommendation.Action == Analyze.Action.TakeProfitsSell)
                {
                    this._logger.Information($"{this.Symbol}: TAKE PROFITS - preparing sell of {assetBalanceToSell} for {aimToGetUSDT.Value}");
                }

                // For increased readability
                var isLastOrderNull = lastOrder == null;
                var isLastOrderBuy = !isLastOrderNull && lastOrder?.Side == OrderSide.Buy;
                var isPanicking = recommendation.Action == Analyze.Action.PanicSell;
                var isLastOrderSellAndPriceIncreased = !isLastOrderNull && lastOrder.Side == OrderSide.Sell
                                                            && lastOrder.Price * requiredPriceGainForResell < bestBidPrice
                                                            && lastOrder.TransactionTime.AddMinutes(5) < DateTime.UtcNow;
                var shallFollowStrongSell = (!this._lastRecommendation.ContainsKey(recommendation.Action)
                                                            || this._lastRecommendation[recommendation.Action].AddMinutes(1) < DateTime.UtcNow)
                                                        && recommendation.Action == Analyze.Action.StrongSell;

                var isLogicValid = aimToGetUSDT.HasValue
                                    && aimToGetUSDT.Value >= symbolInfo.MinNotionalFilter.MinNotional
                                    && bestBidPrice > 0;

                var isPriceRangeValid = aimToGetUSDT.Value >= symbolInfo.PriceFilter.MinPrice
                                        && aimToGetUSDT.Value <= symbolInfo.PriceFilter.MaxPrice;

                var isLOTSizeValid = assetBalanceToSell >= symbolInfo.LotSizeFilter.MinQuantity
                                        && assetBalanceToSell <= symbolInfo.LotSizeFilter.MaxQuantity;

                this._logger.Debug($"{this.Symbol} Sell: {isLastOrderNull} isLastOrderNull");
                this._logger.Debug($"{this.Symbol} Sell: {isLastOrderBuy} isLastOrderBuy");
                this._logger.Debug($"{this.Symbol} Sell: {isPanicking} isPanicking");
                this._logger.Debug($"{this.Symbol} Sell: {isLastOrderSellAndPriceIncreased} isLastOrderSellAndPriceIncreased");
                this._logger.Debug($"{this.Symbol} Sell: {shallFollowStrongSell} shallFollowStrongSell");
                this._logger.Debug($"{this.Symbol} Sell: {isLogicValid} isLogicValid");
                this._logger.Debug($"{this.Symbol} Sell: {isPriceRangeValid} isPriceRangeValid");
                this._logger.Debug($"{this.Symbol} Sell: {isLOTSizeValid} isLOTSizeValid");

                // Check conditions for sell
                if (
                        (
                            isLastOrderNull
                            || isLastOrderBuy
                            || isPanicking
                            || isLastOrderSellAndPriceIncreased
                            || shallFollowStrongSell
                        )
                        // Logic check
                        && isLogicValid
                        // Price range check
                        && isPriceRangeValid
                        // LOT size check
                        && isLOTSizeValid
                    )
                {
                    this._logger.Debug($"{this.Symbol}: Issuing order to sell");

                    if (recommendation.Action == Analyze.Action.PanicSell)
                    {
                        this._logger.Warning($"{this.Symbol}: PANICKING - issuing sell of {assetBalanceToSell} for {aimToGetUSDT.Value}");
                    }
                    else if (recommendation.Action == Analyze.Action.TakeProfitsSell)
                    {
                        this._logger.Warning($"{this.Symbol}: TAKE PROFITS - issuing sell of {assetBalanceToSell} for {aimToGetUSDT.Value}");
                    }

                    // Place the order
                    placedOrder = await this._binanceClient.PlaceOrderAsync(this.Symbol, OrderSide.Sell, OrderType.Market,
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

            return placedOrder;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets latest exchange information
        /// </summary>
        /// <returns></returns>
        private BinanceSymbol _getSymbolInfo()
        {
            return this._buffer.GetSymbolInfoFor(this.Symbol);
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
