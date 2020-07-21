using Binance.Net.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.Market;
using trape.cli.trader.Team;
using trape.datalayer;
using trape.datalayer.Models;
using trape.jobs;
using Action = trape.datalayer.Enums.Action;
using OrderResponseType = trape.datalayer.Enums.OrderResponseType;
using OrderSide = trape.datalayer.Enums.OrderSide;
using OrderType = trape.datalayer.Enums.OrderType;
using TimeInForce = trape.datalayer.Enums.TimeInForce;


namespace trape.cli.trader.Trading
{
    /// <summary>
    /// Does the actual trading taking into account the <c>Recommendation</c> of the <c>Analyst</c> and previous trades
    /// </summary>
    public class Broker : IBroker, IDisposable, IStartable
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
        /// Buffer
        /// </summary>
        private readonly IBuffer _buffer;

        /// <summary>
        /// Job to check if sell/buy is recommended
        /// </summary>
        private readonly Job _jobTrading;

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
        private readonly Dictionary<Action, DateTime> _lastRecommendation;

        /// <summary>
        /// Semaphore synchronizes access in case a task takes longer to return and the timer would elapse again
        /// </summary>
        private readonly SemaphoreSlim _canTrade;

        /// <summary>
        /// Last panic sell
        /// </summary>
        private DateTime _lastPanicSell;

        /// <summary>
        /// Last take profit sell
        /// </summary>
        private DateTime _lastTakeProfitSell;

        /// <summary>
        /// Last jump buy
        /// </summary>
        private DateTime _lastJumpBuy;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Broker</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="buffer">Buffer</param>
        public Broker(ILogger logger, IAccountant accountant, IBuffer buffer)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            this._buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            this._logger = logger.ForContext<Broker>();
            this._cancellationTokenSource = new CancellationTokenSource();
            this._lastRecommendation = new Dictionary<Action, DateTime>();
            this.Symbol = null;
            this._canTrade = new SemaphoreSlim(1, 1);
            this._lastPanicSell = default;
            this._lastTakeProfitSell = default;
            this._lastJumpBuy = default;

            // Set up timer
            this._jobTrading = new Job(new TimeSpan(0, 0, 0, 0, 250), _trading);
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
            var recommendation = this._buffer.GetRecommendation(this.Symbol);
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

                // Buy
                if (recommendation.Action == Action.Buy
                || recommendation.Action == Action.StrongBuy
                || recommendation.Action == Action.JumpBuy)
                {
                    // Buy
                    await this.Buy(recommendation, symbolInfo).ConfigureAwait(true);
                }
                // Sell
                else if (recommendation.Action == Action.Sell
                    || recommendation.Action == Action.StrongSell
                    || recommendation.Action == Action.PanicSell
                    || recommendation.Action == Action.TakeProfitsSell
                    || recommendation.Action == Action.Hold)
                {
                    await this.Sell(recommendation, symbolInfo).ConfigureAwait(true);
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

        #endregion

        #region Trading Methods

        /// <summary>
        /// Runs some checks and places an order if possible
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <param name="symbolInfo">Symbol information</param>
        /// 
        /// <returns>Placed order or null if none placed</returns>
        public async Task Buy(Recommendation recommendation, BinanceSymbol symbolInfo)
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

            // Market 
            var availableUSDT = 0M;

            // During 'Buy' market goes down, so buy for less
            if (recommendation.Action == Action.Buy)
            {
                availableUSDT = usdt.Free * 0.25M;
            }
            else if (recommendation.Action == Action.StrongBuy)
            {
                // During 'StrongBuy' market goes up, buy for 40%
                availableUSDT = usdt.Free * 0.4M;
            }
            else if (recommendation.Action == Action.JumpBuy)
            {
                //  Buy for 20%
                availableUSDT = usdt.Free * 0.2M;
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

            quantity = Math.Round(quantity, symbolInfo.BaseAssetPrecision);

            if (quantity == 0)
            {
                this._logger.Debug($"{this.Symbol}: Quantity is 0");
                return;
            }

            // Get last orders
            PlacedOrder lastOrder = null;
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    lastOrder = database.PlacedOrders
                                .Where(p => p.Symbol == this.Symbol)
                                .OrderByDescending(p => p.TransactionTime).AsNoTracking().FirstOrDefault();
                }
                catch
                {
                    // nothing
                }
            }

            // Logging
            this._logger.Debug($"{this.Symbol} Buy: {recommendation.Action} Bidding:{bestBidPrice:0.00};Quantity:{quantity}");
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

            // Check if last transaction was sell
            var isLastOrderSell = !isLastOrderNull && lastOrder.Side == OrderSide.Sell
                                                    && lastOrder.TransactionTime.AddMinutes(5) < DateTime.UtcNow;

            // Check if last transaction was buy and price decreased
            var isLastOrderBuyAndPriceDecreased = !isLastOrderNull && lastOrder.Side == OrderSide.Buy
                                                    && lastOrder.Price * requiredPriceDropforRebuy > bestBidPrice
                                                    && lastOrder.TransactionTime.AddMinutes(5) < DateTime.UtcNow;

            // Check if time for rebuy
            var isLongTimeNoRebuy = !isLastOrderNull && lastOrder.Side == OrderSide.Buy
                                                    && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow;

            // Check if following strong buy recommendation
            var shallFollowStrongBuy = (!this._lastRecommendation.ContainsKey(recommendation.Action)
                                                        || this._lastRecommendation[recommendation.Action].AddMinutes(1) < DateTime.UtcNow)
                                                    && recommendation.Action == Action.StrongBuy;

            // Check if is jump buy and last one is more than 30 seconds ago
            var isJumpBuy = recommendation.Action == Action.JumpBuy && this._lastJumpBuy.AddSeconds(30) < DateTime.Now;
            if (isJumpBuy)
            {
                this._lastJumpBuy = DateTime.Now;
            }


            var isLogicValid = quantity * bestBidPrice >= symbolInfo.MinNotionalFilter.MinNotional
                                && bestBidPrice > 0;

            var isPriceRangeValid = bestBidPrice >= symbolInfo.PriceFilter.MinPrice
                                        && bestBidPrice <= symbolInfo.PriceFilter.MaxPrice;

            var isLOTSizeValid = quantity >= symbolInfo.LotSizeFilter.MinQuantity
                                    && quantity <= symbolInfo.LotSizeFilter.MaxQuantity;


            this._logger.Verbose($"{this.Symbol} Buy: {isLastOrderNull} isLastOrderNull");
            this._logger.Verbose($"{this.Symbol} Buy: {isLastOrderSell} isLastOrderSell");
            this._logger.Verbose($"{this.Symbol} Buy: {isLastOrderBuyAndPriceDecreased} isLastOrderBuyAndPriceDecreased");
            this._logger.Verbose($"{this.Symbol} Buy: {isLongTimeNoRebuy} isLongTimeNoRebuy");
            this._logger.Verbose($"{this.Symbol} Buy: {shallFollowStrongBuy} shallFollowStrongBuy");
            this._logger.Verbose($"{this.Symbol} Buy: {shallFollowStrongBuy} shallFollowStrongBuy");
            this._logger.Verbose($"{this.Symbol} Buy: {isJumpBuy} isJumpBuy");
            this._logger.Verbose($"{this.Symbol} Buy: {isPriceRangeValid} isPriceRangeValid");
            this._logger.Verbose($"{this.Symbol} Buy: {isLOTSizeValid} isLOTSizeValid");


            // Check conditions for buy
            if (
                    (
                        isLastOrderNull
                        || isLastOrderSell
                        || isLastOrderBuyAndPriceDecreased
                        || isLongTimeNoRebuy
                        || shallFollowStrongBuy
                        || isJumpBuy
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
                var stockExchange = Program.Container.GetService<IStockExchange>();
                await stockExchange.PlaceOrder(new ClientOrder()
                {
                    Symbol = this.Symbol,
                    Side = OrderSide.Buy,
                    Type = (isJumpBuy || bestBidPrice < 50) ? OrderType.Market : OrderType.Limit,
                    OrderResponseType = OrderResponseType.Full,
                    Quantity = quantity,
                    Price = bestBidPrice,
                    TimeInForce = TimeInForce.ImmediateOrCancel,
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
        /// <returns>Placed order or null if none placed</returns>
        public async Task Sell(Recommendation recommendation, BinanceSymbol symbolInfo)
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

                IQueryable<OrderTrade> buyTrades = default;
                // TODO: make scope smaller
                using (AsyncScopedLifestyle.BeginScope(Program.Container))
                {
                    var database = Program.Container.GetService<TrapeContext>();
                    try
                    {
                        // Get last orders
                        var lastOrder = database.PlacedOrders
                                            .Where(p => p.Symbol == this.Symbol && p.ExecutedQuantity > 0)
                                            .OrderByDescending(p => p.TransactionTime).AsNoTracking().FirstOrDefault();

                        // Base query
                        buyTrades = database.PlacedOrders
                                    .Where(p => p.Side == OrderSide.Buy
                                        && p.Symbol == this.Symbol
                                        && p.ExecutedQuantity > 0)
                                    .SelectMany(f => f.Fills.Where(f => f.Quantity > f.ConsumedQuantity)).AsNoTracking();

                        // Normal quantity
                        var margin = recommendation.Action == Action.StrongSell ? 1.001M : 1.003M;
                        var normalQuantity = buyTrades
                                                        .Where(f =>
                                                            /* Bid price is 0.2% higher */
                                                            f.Price * margin < bestAskPrice
                                                            && f.Quantity > f.ConsumedQuantity)
                                                    .Sum(f => (f.Quantity - f.ConsumedQuantity));

                        // Sell what is maximal possible (from what was bought or (in case of discrepancy between recorded and actual value) what is possible
                        var multiplier = recommendation.Action == Action.StrongSell ? 0.8M : 0.6M;
                        var quantity = Math.Min(assetBalance.Free * multiplier, normalQuantity);

                        // Panic Mode, sell everything
                        if (recommendation.Action == Action.PanicSell)
                        {
                            // Sell 70% of assets where price gained 0.2%
                            quantity = buyTrades
                                            .Where(f =>
                                                (f.Price * 1.002M) < bestAskPrice
                                                && f.Quantity > f.ConsumedQuantity)
                                        .Sum(f => (f.Quantity - f.ConsumedQuantity)) * 0.7M;
                        }
                        else if (recommendation.Action == Action.TakeProfitsSell)
                        {
                            // Take 70% of what is available for the profit sell, sometimes it rises again afterwards
                            quantity = buyTrades
                                            .Where(f =>
                                                (f.Price / Analyst.TakeProfitLimit) < bestAskPrice
                                                && f.Quantity > f.ConsumedQuantity)
                                        .Sum(f => (f.Quantity - f.ConsumedQuantity)) * 0.8M;
                        }

                        // Wait until previous trades were handled
                        var lockedOpenOrderQuantity = this._buffer.GetOpenOrderValue(this.Symbol);
                        quantity = lockedOpenOrderQuantity == 0 ? quantity : 0;

                        quantity = Math.Round(quantity, symbolInfo.BaseAssetPrecision);

                        if (quantity == 0)
                        {
                            this._logger.Debug($"{this.Symbol}: Quantity is 0");
                            return;
                        }


                        // Logging
                        this._logger.Debug($"{this.Symbol} Sell: {recommendation.Action};Asking:{bestAskPrice:0.00};Free:{assetBalance?.Free:0.00######};Quantity:{quantity:0.00######};LockedOpenOrderAmount:{lockedOpenOrderQuantity:0.00}");
                        this._logger.Verbose($"{this.Symbol} Sell: Checking conditions");
                        this._logger.Verbose($"{this.Symbol} Sell: {null == lastOrder} lastOrder is null");
                        this._logger.Verbose($"{this.Symbol} Sell: {lastOrder?.Side.ToString()} lastOrder side");
                        this._logger.Verbose($"{this.Symbol} Sell: {lastOrder?.Price * requiredPriceGainForResell < bestAskPrice} lastOrder Price: {lastOrder?.Price} * {requiredPriceGainForResell} < {bestAskPrice}");
                        this._logger.Verbose($"{this.Symbol} Sell: {lastOrder?.TransactionTime.AddMinutes(15) < DateTime.UtcNow} Transaction Time: {lastOrder?.TransactionTime} + 15 minutes ({lastOrder?.TransactionTime.AddMinutes(5)}) < {DateTime.UtcNow}");
                        this._logger.Verbose($"{this.Symbol} Sell: {quantity:0.00000000} quantity");
                        this._logger.Verbose($"{this.Symbol} Sell: {recommendation.Action} recommendation");
                        this._logger.Verbose($"{this.Symbol} Sell: {quantity * bestAskPrice >= symbolInfo.MinNotionalFilter.MinNotional} Value {quantity * bestAskPrice} > 0: {quantity * bestAskPrice > 0} and higher than {symbolInfo.MinNotionalFilter.MinNotional}");
                        this._logger.Verbose($"{this.Symbol} Sell: {symbolInfo.PriceFilter.MaxPrice >= bestAskPrice && bestAskPrice >= symbolInfo.PriceFilter.MinPrice} MaxPrice { symbolInfo.PriceFilter.MaxPrice} > Amount {bestAskPrice} > MinPrice {symbolInfo.PriceFilter.MinPrice}");
                        this._logger.Verbose($"{this.Symbol} Sell: {symbolInfo.LotSizeFilter.MaxQuantity >= quantity && quantity >= symbolInfo.LotSizeFilter.MinQuantity} MaxLOT {symbolInfo.LotSizeFilter.MaxQuantity} > Amount {quantity} > MinLOT {symbolInfo.LotSizeFilter.MinQuantity}");

                        // TODO: adjust logging to log only failing step and returning
                        // TODO: if-direct checks
                        // For increased readability
                        var isLastOrderNull = lastOrder == null;

                        // Check if last transaction was buy
                        var isLastOrderBuy = !isLastOrderNull && lastOrder?.Side == OrderSide.Buy
                                                                && lastOrder.TransactionTime.AddMinutes(20) < DateTime.UtcNow;

                        // Check if is panick sell but only panic once every 20 seconds
                        var isPanicking = recommendation.Action == Action.PanicSell && this._lastPanicSell.AddSeconds(20) < DateTime.UtcNow;
                        if (isPanicking)
                        {
                            this._lastPanicSell = DateTime.UtcNow;
                        }

                        // Check if last transaction was also sell but price has gained and transaction is older than 5 minutes
                        var isLastOrderSellAndPriceIncreased = !isLastOrderNull && lastOrder.Side == OrderSide.Sell
                                                                    && lastOrder.Price * requiredPriceGainForResell < bestAskPrice
                                                                    && lastOrder.TransactionTime.AddMinutes(7) < DateTime.UtcNow;

                        // Only one strong sell per minute
                        var shallFollowStrongSell = (!this._lastRecommendation.ContainsKey(recommendation.Action)
                                                                    || this._lastRecommendation[recommendation.Action].AddMinutes(5) < DateTime.UtcNow)
                                                                && recommendation.Action == Action.StrongSell;

                        // Only one panic sell per minute
                        var shallFollowPanicSell = (!this._lastRecommendation.ContainsKey(recommendation.Action)
                                                                    || this._lastRecommendation[recommendation.Action].AddMinutes(2) < DateTime.UtcNow)
                                                                && recommendation.Action == Action.PanicSell;

                        // Check if we just take profits
                        var isTakeProfitSell = recommendation.Action == Action.TakeProfitsSell && this._lastTakeProfitSell.AddSeconds(20) < DateTime.Now;
                        if (isTakeProfitSell)
                        {
                            this._lastTakeProfitSell = DateTime.UtcNow;
                        }

                        var isLogicValid = quantity * bestAskPrice >= symbolInfo.MinNotionalFilter.MinNotional
                                            && bestAskPrice > 0;

                        var isPriceRangeValid = bestAskPrice >= symbolInfo.PriceFilter.MinPrice
                                                && bestAskPrice <= symbolInfo.PriceFilter.MaxPrice;

                        var isLOTSizeValid = quantity >= symbolInfo.LotSizeFilter.MinQuantity
                                                && quantity <= symbolInfo.LotSizeFilter.MaxQuantity;

                        // Last check that amount is available
                        var isAmountAvailable = assetBalance.Free >= quantity;


                        this._logger.Verbose($"{this.Symbol} Sell: {isLastOrderNull} isLastOrderNull");
                        this._logger.Verbose($"{this.Symbol} Sell: {isLastOrderBuy} isLastOrderBuy");
                        this._logger.Verbose($"{this.Symbol} Sell: {isPanicking} isPanicking");
                        this._logger.Verbose($"{this.Symbol} Sell: {isLastOrderSellAndPriceIncreased} isLastOrderSellAndPriceIncreased");
                        this._logger.Verbose($"{this.Symbol} Sell: {shallFollowStrongSell} shallFollowStrongSell");
                        this._logger.Verbose($"{this.Symbol} Sell: {shallFollowPanicSell} shallFollowPanicSell");
                        this._logger.Verbose($"{this.Symbol} Sell: {isTakeProfitSell} isTakeProfitSell");
                        this._logger.Verbose($"{this.Symbol} Sell: {isLogicValid} isLogicValid");
                        this._logger.Verbose($"{this.Symbol} Sell: {isPriceRangeValid} isPriceRangeValid");
                        this._logger.Verbose($"{this.Symbol} Sell: {isLOTSizeValid} isLOTSizeValid");
                        this._logger.Verbose($"{this.Symbol} Sell: {isAmountAvailable} isAmountAvailable");

                        // Check conditions for sell
                        if (
                                (
                                    isLastOrderNull
                                    || isLastOrderBuy
                                    || isPanicking
                                    || isLastOrderSellAndPriceIncreased
                                    || shallFollowStrongSell
                                    || shallFollowPanicSell
                                    || isTakeProfitSell
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
                            // Get stock exchange and place order
                            var stockExchange = Program.Container.GetInstance<IStockExchange>();
                            await stockExchange.PlaceOrder(new ClientOrder()
                            {
                                Symbol = this.Symbol,
                                Side = OrderSide.Sell,
                                Type = (isTakeProfitSell || shallFollowPanicSell || bestAskPrice < 50) ? OrderType.Market : OrderType.Limit,
                                OrderResponseType = OrderResponseType.Full,
                                Quantity = quantity,
                                Price = bestAskPrice,
                                TimeInForce = TimeInForce.ImmediateOrCancel
                            }, this._cancellationTokenSource.Token).ConfigureAwait(true);
                        }
                        else
                        {
                            this._logger.Debug($"{this.Symbol}: Skipping, final conditions not met");
                        }
                    }
                    catch (Exception e)
                    {
                        this._logger.Error(e, e.Message);
                    }
                }
            }
        }

        #endregion

        #region Start / Terminate

        /// <summary>
        /// Start the Broker
        /// </summary>
        /// <param name="symbol">Symbol to trade</param>
        public void Start(string symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(paramName: symbol);

            if (this._jobTrading.Enabled)
            {
                this._logger.Warning($"{symbol}: Broker is already active");
                return;
            }

            this._logger.Information($"{symbol}: Starting Broker");

            this.Symbol = symbol;

            this._jobTrading.Start();

            this._logger.Information($"{symbol}: Broker started");
        }

        /// <summary>
        /// Terminates the broker
        /// </summary>
        /// <returns></returns>
        public async Task Terminate()
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
