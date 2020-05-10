using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Objects;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using Microsoft.Extensions.DependencyInjection;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.Market;
using trape.cli.trader.WatchDog;
using trape.datalayer;
using trape.datalayer.Models;
using trape.jobs;
using Microsoft.EntityFrameworkCore;

namespace trape.cli.trader.Trading
{
    /// <summary>
    /// Does the actual trading taking into account the <c>Recommendation</c> of the <c>Analyst</c> and previous trades
    /// </summary>
    public class Broker : IBroker, IActive
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
        /// Job to check if sell/buy is recommended
        /// </summary>
        private Job _jobTrading;

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
        /// Last time <c>Broker</c> was active
        /// </summary>
        public DateTime LastActive { get; private set; }

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Records last strongs to not execute a strong <c>Recommendation</c> one after the other without a break inbetween
        /// </summary>
        private readonly Dictionary<datalayer.Enums.Action, DateTime> _lastRecommendation;

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
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _ = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            _ = analyst ?? throw new ArgumentNullException(paramName: nameof(analyst));

            _ = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _ = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            #endregion

            this._logger = logger.ForContext<Broker>();
            this._accountant = accountant;
            this._analyst = analyst;
            this._buffer = buffer;
            this._binanceClient = binanceClient;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._lastRecommendation = new Dictionary<datalayer.Enums.Action, DateTime>();
            this.Symbol = null;
            this._canTrade = new SemaphoreSlim(1, 1);

            // Set up timer
            this._jobTrading = new Job(new TimeSpan(0, 0, 0, 0, 100), _trading);
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Checks the <c>Recommendation</c> of the <c>Analyst</c> and previous trades and decides whether to buy, wait or sell
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _trading()
        {
            this.LastActive = DateTime.UtcNow;

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
            else if (recommendation.Action == datalayer.Enums.Action.Hold)
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
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    // Wait because context is available, otherwise would have exited before reaching this step
                    // Synchronizing is just used in the rare case that a method needs longer to return than the timer needs to elapse
                    // so that no other task runs the same method
                    this._canTrade.Wait();

                    // Get min notional
                    var symbolInfo = this._buffer.GetSymbolInfoFor(this.Symbol);

                    // If exchange information are not yet available, return
                    if (symbolInfo == null)
                    {
                        return;
                    }

                    // Generate new client order id
                    var newClientOrderId = Guid.NewGuid().ToString("N");


                    // Get last orders
                    var lastOrder = database.PlacedOrders
                                        .Where(p => p.Symbol == this.Symbol)
                                        .OrderByDescending(p => p.TransactionTime).FirstOrDefault();

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

                    // Buy
                    if (recommendation.Action == datalayer.Enums.Action.Buy
                        || recommendation.Action == datalayer.Enums.Action.StrongBuy)
                    {
                        // Buy
                        await this.Buy(recommendation, symbolInfo, lastOrder).ConfigureAwait(true);
                    }
                    // Sell
                    else if (recommendation.Action == datalayer.Enums.Action.Sell
                        || recommendation.Action == datalayer.Enums.Action.StrongSell
                        || recommendation.Action == datalayer.Enums.Action.PanicSell
                        || recommendation.Action == datalayer.Enums.Action.TakeProfitsSell)
                    {
                        await this.Sell(recommendation, symbolInfo, lastOrder).ConfigureAwait(true);
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
                }
                // Catch All Exception
                catch (Exception cae)
                {
                    this._logger.Error(cae, cae.Message);
                }
                finally
                {
                    // Release lock
                    this._canTrade.Release();
                }
            }
        }

        #endregion

        #region Trading Methods

        /// <summary>
        /// Runs some checks and places an order if possible
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <param name="symbolInfo">Symbol information</param>
        /// <param name="database">Database conneciton</param>
        /// <param name="lastOrder">Last order, if any</param>
        /// 
        /// <returns>Placed order or null if none placed</returns>
        public async Task Buy(Recommendation recommendation, BinanceSymbol symbolInfo, PlacedOrder lastOrder = null)
        {
            #region Argument checks

            _ = recommendation ?? throw new ArgumentNullException(paramName: nameof(recommendation));

            _ = symbolInfo ?? throw new ArgumentNullException(paramName: nameof(symbolInfo));

            #endregion

            this._logger.Debug($"{this.Symbol}: Preparing to buy");

            // Get remaining USDT balance for trading
            var usdt = await this._accountant.GetBalance("USDT").ConfigureAwait(false);
            if (null == usdt)
            {
                // Something is oddly wrong, wait a bit
                this._logger.Debug("Cannot retrieve account info");
                return;
            }

            // Only take half of what is available to buy assets
            var availableUSDT = usdt.Free * 0.6M;
            // In case of strong buy increase budget
            if (recommendation.Action == datalayer.Enums.Action.StrongBuy)
            {
                availableUSDT = usdt.Free * 0.8M;
            }

            // Round to a valid value
            //  symbolInfo.BaseAssetPrecision, MidpointRounding.ToZero
            availableUSDT = Math.Round(availableUSDT, 2, MidpointRounding.ToZero);

            if (availableUSDT == 0)
            {
                this._logger.Debug($"{this.Symbol}: No funds available");
                return;
            }

            // Get ask price
            var bestBidPrice = this._buffer.GetBidPrice(this.Symbol);

            var quantity = availableUSDT / bestBidPrice;

            // Logging
            this._logger.Debug($"{this.Symbol} Buy: {recommendation.Action} bestBidPrice:{bestBidPrice:0.00};quantity:{quantity}");
            this._logger.Verbose($"{this.Symbol} Buy: Checking conditions");
            this._logger.Verbose($"{this.Symbol} Buy: {null == lastOrder} lastOrder is null");
            this._logger.Verbose($"{this.Symbol} Buy: {lastOrder?.Side.ToString()} lastOrder side");
            this._logger.Verbose($"{this.Symbol} Buy: {lastOrder?.Price * requiredPriceDropforRebuy > bestBidPrice} lastOrder Price: {lastOrder?.Price} * {requiredPriceDropforRebuy} > {bestBidPrice}");
            this._logger.Verbose($"{this.Symbol} Buy: {lastOrder?.TransactionTime.AddMinutes(15) < DateTime.UtcNow} Transaction Time: {lastOrder?.TransactionTime} + 15 minutes ({lastOrder?.TransactionTime.AddMinutes(5)}) < {DateTime.UtcNow}");
            this._logger.Verbose($"{this.Symbol} Buy: {quantity} quantity");
            this._logger.Verbose($"{this.Symbol} Buy: {recommendation.Action} recommendation");
            this._logger.Verbose($"{this.Symbol} Buy: {quantity * bestBidPrice >= symbolInfo.MinNotionalFilter.MinNotional} Value {quantity * bestBidPrice} > 0: {quantity * bestBidPrice > 0} and higher than minNotional {symbolInfo.MinNotionalFilter.MinNotional}");
            this._logger.Verbose($"{this.Symbol} Buy: {symbolInfo.PriceFilter.MaxPrice >= bestBidPrice && bestBidPrice >= symbolInfo.PriceFilter.MinPrice} MaxPrice {symbolInfo.PriceFilter.MaxPrice} >= Amount {bestBidPrice} >= MinPrice {symbolInfo.PriceFilter.MinPrice}");
            this._logger.Verbose($"{this.Symbol} Buy: {symbolInfo.LotSizeFilter.MaxQuantity >= quantity && quantity >= symbolInfo.LotSizeFilter.MinQuantity} MaxLOT {symbolInfo.LotSizeFilter.MaxQuantity} > Amount {quantity} > MinLOT {symbolInfo.LotSizeFilter.MinQuantity}");


            // For increased readability
            var isLastOrderNull = lastOrder == null;
            
            var isLastOrderSell = !isLastOrderNull && lastOrder.Side == datalayer.Enums.OrderSide.Sell;
            
            var isLastOrderBuyAndPriceDecreased = !isLastOrderNull && lastOrder.Side == datalayer.Enums.OrderSide.Buy
                                                    && lastOrder.Price * requiredPriceDropforRebuy > bestBidPrice
                                                    && lastOrder.TransactionTime.AddMinutes(5) < DateTime.UtcNow;

            var isLongTimeNoRebuy = !isLastOrderNull && lastOrder.Side == datalayer.Enums.OrderSide.Buy
                                                    && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow;

            var shallFollowStrongBuy = (!this._lastRecommendation.ContainsKey(recommendation.Action)
                                                        || this._lastRecommendation[recommendation.Action].AddMinutes(1) < DateTime.UtcNow)
                                                    && recommendation.Action == datalayer.Enums.Action.StrongBuy;

            var isLogicValid = quantity * bestBidPrice >= symbolInfo.MinNotionalFilter.MinNotional
                                && bestBidPrice > 0;

            var isPriceRangeValid = bestBidPrice >= symbolInfo.PriceFilter.MinPrice
                                        && bestBidPrice <= symbolInfo.PriceFilter.MaxPrice;

            var isLOTSizeValid = quantity >= symbolInfo.LotSizeFilter.MinQuantity
                                    && quantity <= symbolInfo.LotSizeFilter.MaxQuantity;

            this._logger.Debug($"{this.Symbol} Buy: {isLastOrderNull} isLastOrderNull");
            this._logger.Debug($"{this.Symbol} Buy: {isLastOrderSell} isLastOrderSell");
            this._logger.Debug($"{this.Symbol} Buy: {isLastOrderBuyAndPriceDecreased} isLastOrderBuyAndPriceDecreased");
            this._logger.Debug($"{this.Symbol} Buy: {isLongTimeNoRebuy} isLongTimeNoRebuy");
            this._logger.Debug($"{this.Symbol} Buy: {shallFollowStrongBuy} shallFollowStrongBuy");
            this._logger.Debug($"{this.Symbol} Buy: {isLogicValid} isLogicValid");
            this._logger.Debug($"{this.Symbol} Buy: {isPriceRangeValid} isPriceRangeValid");
            this._logger.Debug($"{this.Symbol} Buy: {isLOTSizeValid} isLOTSizeValid");

            // Check conditions for buy
            if (
                    (
                    isLastOrderNull
                    || isLastOrderSell
                    || isLastOrderBuyAndPriceDecreased
                    || isLongTimeNoRebuy
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
                // Get stock exchange and place order
                var stockExchange = Program.Container.GetInstance<IStockExchange>();
                await stockExchange.PlaceOrder(new ClientOrder()
                {
                    Symbol = this.Symbol,
                    Side = datalayer.Enums.OrderSide.Buy,
                    Type = datalayer.Enums.OrderType.Limit,
                    OrderResponseType = datalayer.Enums.OrderResponseType.Full,
                    Quantity = quantity,
                    Price = bestBidPrice,
                    TimeInForce = datalayer.Enums.TimeInForce.ImmediateOrCancel
                }, this._cancellationTokenSource.Token).ConfigureAwait(true);
            }
            else
            {
                this._logger.Debug($"{this.Symbol}: Skipping, final conditions not met");
            }
        }

        /// <summary>
        /// Runs some checks and places an order if possible
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <param name="symbolInfo">Symbol info</param>
        /// <param name="lastOrder">Last order, if any</param>
        /// <returns>Placed order or null if none placed</returns>
        public async Task Sell(Recommendation recommendation, BinanceSymbol symbolInfo, PlacedOrder lastOrder = null)
        {
            #region Argument checks

            _ = recommendation ?? throw new ArgumentNullException(paramName: nameof(recommendation));

            _ = symbolInfo ?? throw new ArgumentNullException(paramName: nameof(symbolInfo));

            #endregion

            this._logger.Debug($"{this.Symbol}: Preparing to sell");

            var assetBalance = await this._accountant.GetBalance(this.Symbol.Replace("USDT", string.Empty)).ConfigureAwait(true);

            if (assetBalance == null || assetBalance?.Free == 0)
            {
                this._logger.Debug($"{this.Symbol} nothing free");

                if (recommendation.Action == datalayer.Enums.Action.PanicSell)
                {
                    this._logger.Warning($"{this.Symbol}: PANICKING - but no asset free to sell");
                }
            }
            else
            {
                // Get best bid price
                var bestAskPrice = this._buffer.GetAskPrice(this.Symbol);

                // Normal - Do not sell below buying price
                // - Where buying price * 1.002 (0.2%) is smaller than bidding price
                // Select quantities
                IQueryable<OrderTrade> buyTrades = default;
                // TODO: make scope smaller
                using (AsyncScopedLifestyle.BeginScope(Program.Container))
                {
                    var database = Program.Container.GetService<TrapeContext>();
                    try
                    {
                        // Base query
                        buyTrades = database.PlacedOrders
                                    .Where(p => p.Side == datalayer.Enums.OrderSide.Buy
                                        && p.Symbol == this.Symbol)
                                    .SelectMany(f => f.Fills.Where(f => f.Quantity > f.ConsumedQuantity));

                        // Normal quantity
                        var normalQuantity = buyTrades
                                                        .Where(f =>
                                                            /* Bid price is 0.2% higher */
                                                            f.Price * 1.002M < bestAskPrice)
                                                    .Sum(f => (f.Quantity - f.ConsumedQuantity));

                        // Stop-Loss - Limit loss
                        // - Where buying-price was 0.5% higher than current price
                        // And where assets are available and side is buy
                        // Select quantities
                        var stopLossQuantity = buyTrades
                                                .Where(l =>  /* Stop-Loss: 1.0% lower */
                                                            l.Price > bestAskPrice * 1.01M && !(l.Price * 1.002M < bestAskPrice)
                                                                && recommendation.Action == datalayer.Enums.Action.StrongSell)
                                                            .Sum(l => (l.Quantity - l.ConsumedQuantity));

                        // Reduce by 1% due to possible differences between database and Binance
                        var sellAssetQuantity = (stopLossQuantity + normalQuantity) * 0.99M;

                        // Sell 65% of the asset
                        var quantity = assetBalance.Free * 0.65M;

                        // Sell what is maximal possible (from what was bought or (in case of discrepancy between recorded and actual value) what is possible
                        quantity = Math.Min(quantity, sellAssetQuantity);

                        // Panic Mode, sell everything
                        if (recommendation.Action == datalayer.Enums.Action.PanicSell)
                        {
                            // Take 80%
                            quantity = assetBalance.Free * 0.8M;

                            // Sell 0.25% under value
                            bestAskPrice = bestAskPrice * 0.9975M;
                        }

                        // Wait until previous trades were handled
                        var lockedOpenOrderAmount = this._buffer.GetOpenOrderValue(this.Symbol);
                        quantity = lockedOpenOrderAmount == 0 ? quantity : 0;

                        if (quantity == 0)
                        {
                            this._logger.Debug($"{this.Symbol}: Quantity is 0");
                            return;
                        }


                        // Logging
                        this._logger.Debug($"{this.Symbol} Sell: {recommendation.Action} bestAskPrice:{Math.Round(bestAskPrice, symbolInfo.BaseAssetPrecision)};assetBalance.Free:{assetBalance?.Free:0.00};sellQuoteOrderQuantity:{quantity:0.00};quantity:{quantity:0.00};lockedOpenOrderAmount:{lockedOpenOrderAmount:0.00}");
                        this._logger.Verbose($"{this.Symbol} Sell: Checking conditions");
                        this._logger.Verbose($"{this.Symbol} Sell: {null == lastOrder} lastOrder is null");
                        this._logger.Verbose($"{this.Symbol} Sell: {lastOrder?.Side.ToString()} lastOrder side");
                        this._logger.Verbose($"{this.Symbol} Sell: {lastOrder?.Price * requiredPriceGainForResell < bestAskPrice} lastOrder Price: {lastOrder?.Price} * {requiredPriceGainForResell} < {bestAskPrice}");
                        this._logger.Verbose($"{this.Symbol} Sell: {lastOrder?.TransactionTime.AddMinutes(15) < DateTime.UtcNow} Transaction Time: {lastOrder?.TransactionTime} + 15 minutes ({lastOrder?.TransactionTime.AddMinutes(5)}) < {DateTime.UtcNow}");
                        this._logger.Verbose($"{this.Symbol} Sell: {quantity} assetBalanceToSell");
                        this._logger.Verbose($"{this.Symbol} Sell: {recommendation.Action} recommendation");
                        this._logger.Verbose($"{this.Symbol} Sell: {stopLossQuantity:0.0000} Stop-Loss quantity");
                        this._logger.Verbose($"{this.Symbol} Sell: {quantity * bestAskPrice >= symbolInfo.MinNotionalFilter.MinNotional} Value {quantity * bestAskPrice} > 0: {quantity * bestAskPrice > 0} and higher than {symbolInfo.MinNotionalFilter.MinNotional}");
                        this._logger.Verbose($"{this.Symbol} Sell: {symbolInfo.PriceFilter.MaxPrice >= bestAskPrice && bestAskPrice >= symbolInfo.PriceFilter.MinPrice} MaxPrice { symbolInfo.PriceFilter.MaxPrice} > Amount {bestAskPrice} > MinPrice {symbolInfo.PriceFilter.MinPrice}");
                        this._logger.Verbose($"{this.Symbol} Sell: {symbolInfo.LotSizeFilter.MaxQuantity >= quantity && quantity >= symbolInfo.LotSizeFilter.MinQuantity} MaxLOT {symbolInfo.LotSizeFilter.MaxQuantity} > Amount {quantity} > MinLOT {symbolInfo.LotSizeFilter.MinQuantity}");

                        // TODO: adjust logging to log only failing step and returning
                        // TODO: if-direct checks
                        // For increased readability
                        var isLastOrderNull = lastOrder == null;
                        
                        var isLastOrderBuy = !isLastOrderNull && lastOrder.Side == datalayer.Enums.OrderSide.Buy;
                        
                        var isPanicking = recommendation.Action == datalayer.Enums.Action.PanicSell;
                        
                        var isLastOrderSellAndPriceIncreased = !isLastOrderNull && lastOrder.Side == datalayer.Enums.OrderSide.Sell
                                                                    && lastOrder.Price * requiredPriceGainForResell < bestAskPrice
                                                                    && lastOrder.TransactionTime.AddMinutes(5) < DateTime.UtcNow;

                        var shallFollowStrongSell = (!this._lastRecommendation.ContainsKey(recommendation.Action)
                                                                    || this._lastRecommendation[recommendation.Action].AddMinutes(1) < DateTime.UtcNow)
                                                                && recommendation.Action == datalayer.Enums.Action.StrongSell;

                        var isLogicValid = quantity * bestAskPrice >= symbolInfo.MinNotionalFilter.MinNotional
                                            && bestAskPrice > 0;

                        var isPriceRangeValid = bestAskPrice >= symbolInfo.PriceFilter.MinPrice
                                                && bestAskPrice <= symbolInfo.PriceFilter.MaxPrice;

                        var isLOTSizeValid = quantity >= symbolInfo.LotSizeFilter.MinQuantity
                                                && quantity <= symbolInfo.LotSizeFilter.MaxQuantity;

                        var hasStopLossQuantity = stopLossQuantity > 0;

                        // Last check that amount is available
                        var isAmountAvailable = assetBalance.Free > quantity;


                        this._logger.Debug($"{this.Symbol} Sell: {isLastOrderNull} isLastOrderNull");
                        this._logger.Debug($"{this.Symbol} Sell: {isLastOrderBuy} isLastOrderBuy");
                        this._logger.Debug($"{this.Symbol} Sell: {isPanicking} isPanicking");
                        this._logger.Debug($"{this.Symbol} Sell: {isLastOrderSellAndPriceIncreased} isLastOrderSellAndPriceIncreased");
                        this._logger.Debug($"{this.Symbol} Sell: {shallFollowStrongSell} shallFollowStrongSell");
                        this._logger.Debug($"{this.Symbol} Sell: {isLogicValid} isLogicValid");
                        this._logger.Debug($"{this.Symbol} Sell: {isPriceRangeValid} isPriceRangeValid");
                        this._logger.Debug($"{this.Symbol} Sell: {isLOTSizeValid} isLOTSizeValid");
                        this._logger.Debug($"{this.Symbol} Sell: {hasStopLossQuantity} hasStopLossQuantity");
                        this._logger.Debug($"{this.Symbol} Sell: {isAmountAvailable} isAmountAvailable");

                        // Check conditions for sell
                        if (
                                (
                                    isLastOrderNull
                                    || isLastOrderBuy
                                    || isPanicking
                                    || isLastOrderSellAndPriceIncreased
                                    || shallFollowStrongSell
                                    || hasStopLossQuantity
                                )
                                // Logic check
                                && isLogicValid
                                // Price range check
                                && isPriceRangeValid
                                // LOT size check
                                && isLOTSizeValid
                                && isAmountAvailable
                            )
                        {
                            if (hasStopLossQuantity)
                            {
                                this._logger.Information($"{this.Symbol} @ {bestAskPrice:0.00}: StopLoss - Issuing sell of {stopLossQuantity} / {quantity}");
                            }

                            // Get stock exchange and place order
                            var stockExchange = Program.Container.GetInstance<IStockExchange>();
                            await stockExchange.PlaceOrder(new ClientOrder()
                            {
                                Symbol = this.Symbol,
                                Side = datalayer.Enums.OrderSide.Sell,
                                Type = datalayer.Enums.OrderType.Limit,
                                OrderResponseType = datalayer.Enums.OrderResponseType.Full,
                                Quantity = quantity,
                                Price = bestAskPrice,
                                TimeInForce = datalayer.Enums.TimeInForce.ImmediateOrCancel
                            }, this._cancellationTokenSource.Token).ConfigureAwait(true);
                        }
                        else
                        {
                            this._logger.Debug($"{this.Symbol}: Skipping, final conditions not met");
                        }
                    }
                    catch (Exception e)
                    {
                        this._logger.Error(e.Message, e);
                    }
                }
            }
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Start the Broker
        /// </summary>
        /// <param name="symbolToTrade">Symbol to trade</param>
        public void Start(string symbolToTrade)
        {
            if (this._jobTrading.Enabled)
            {
                this._logger.Warning($"{symbolToTrade}: Broker is already active");
                return;
            }

            this._logger.Information($"{symbolToTrade}: Starting Broker");

            if (this._buffer.GetSymbols().Contains(symbolToTrade))
            {
                this.Symbol = symbolToTrade;

                this._jobTrading.Start();

                this._logger.Information($"{this.Symbol}: Broker started");
            }
            else
            {
                this._logger.Error($"{symbolToTrade}: Broker cannot be started, symbol does not exist");
            }
        }

        /// <summary>
        /// Finish the broker
        /// </summary>
        /// <returns></returns>
        public async Task Finish()
        {
            if (!this._jobTrading.Enabled)
            {
                this._logger.Warning($"{this.Symbol}: Broker is not active");
                return;
            }

            this._logger.Information($"{this.Symbol}: Stopping Broker");

            this._jobTrading.Terminate();

            // Give time for running task to end
            await Task.Delay(1000).ConfigureAwait(false);

            this._cancellationTokenSource.Cancel();

            this._logger.Information($"{this.Symbol}: Broker stopped");
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
                this._jobTrading.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
