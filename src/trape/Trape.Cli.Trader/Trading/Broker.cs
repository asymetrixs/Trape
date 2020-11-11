using Binance.Net.Objects.Spot.MarketData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.trader.Account;
using Trape.Cli.trader.Analyze;
using Trape.Cli.trader.Cache;
using Trape.Cli.trader.Market;
using Trape.Cli.trader.Team;
using Trape.Datalayer;
using Trape.Datalayer.Models;
using Trape.Jobs;
using Action = Trape.Datalayer.Enums.Action;
using OrderResponseType = Trape.Datalayer.Enums.OrderResponseType;
using OrderSide = Trape.Datalayer.Enums.OrderSide;
using OrderType = Trape.Datalayer.Enums.OrderType;
using TimeInForce = Trape.Datalayer.Enums.TimeInForce;


namespace Trape.Cli.trader.Trading
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

            _accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            _buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            _logger = logger.ForContext<Broker>();
            _cancellationTokenSource = new CancellationTokenSource();
            _lastRecommendation = new Dictionary<Action, DateTime>();
            Symbol = null;
            _canTrade = new SemaphoreSlim(1, 1);
            _lastPanicSell = default;
            _lastTakeProfitSell = default;
            _lastJumpBuy = default;

            // Set up timer
            _jobTrading = new Job(new TimeSpan(0, 0, 0, 0, 250), _trading);
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
            LastActive = DateTime.UtcNow;

            // Check if Symbol is set
            if (string.IsNullOrEmpty(Symbol))
            {
                _logger.Error("Trying to trade with empty symbol! Aborting...");
                return;
            }

            _logger.Verbose($"{Symbol}: Going to trade");

            // Get recommendation
            var recommendation = _buffer.GetRecommendation(Symbol);
            if (null == recommendation)
            {
                _logger.Verbose($"{Symbol}: No recommendation available.");
                return;
            }
            else if (recommendation.Action == Datalayer.Enums.Action.Hold)
            {
                _logger.Verbose($"{Symbol}: Recommendation is hold.");
                return;
            }

            // If this point is reached, the preconditions are fine, check if slot is available
            if (_canTrade.CurrentCount == 0)
            {
                return;
            }

            try
            {
                // Wait because context is available, otherwise would have exited before reaching this step
                // Synchronizing is just used in the rare case that a method needs longer to return than the timer needs to elapse
                // so that no other task runs the same method
                _canTrade.Wait();

                // Get min notional
                var symbolInfo = _buffer.GetSymbolInfoFor(Symbol);

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
                    await Buy(recommendation, symbolInfo).ConfigureAwait(true);
                }
                // Sell
                else if (recommendation.Action == Action.Sell
                    || recommendation.Action == Action.StrongSell
                    || recommendation.Action == Action.PanicSell
                    || recommendation.Action == Action.TakeProfitsSell
                    || recommendation.Action == Action.Hold)
                {
                    await Sell(recommendation, symbolInfo).ConfigureAwait(true);
                }

                // Log recommended action, use for strong recommendations
                if (_lastRecommendation.ContainsKey(recommendation.Action))
                {
                    _lastRecommendation[recommendation.Action] = DateTime.UtcNow;
                }
                else
                {
                    _lastRecommendation.Add(recommendation.Action, DateTime.UtcNow);
                }
            }
            // Catch All Exception
            catch (Exception cae)
            {
                _logger.Error(cae, cae.Message);
            }
            finally
            {
                // Release lock
                _canTrade.Release();
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

            _logger.Debug($"{Symbol}: Preparing to buy");

            // Get remaining USDT balance for trading
            var usdt = await _accountant.GetBalance("USDT").ConfigureAwait(false);
            if (null == usdt)
            {
                // Something is oddly wrong, wait a bit
                _logger.Debug("Cannot retrieve account info");
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
                _logger.Debug($"{Symbol}: No funds available");
                return;
            }

            // Get ask price
            var bestBidPrice = _buffer.GetBidPrice(Symbol);

            var quantity = availableUSDT / bestBidPrice;

            quantity = Math.Round(quantity, symbolInfo.BaseAssetPrecision);

            if (quantity == 0)
            {
                _logger.Debug($"{Symbol}: Quantity is 0");
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
                                .Where(p => p.Symbol == Symbol)
                                .OrderByDescending(p => p.TransactionTime).AsNoTracking().FirstOrDefault();
                }
                catch
                {
                    // nothing
                }
            }

            // Logging
            _logger.Debug($"{Symbol} Buy: {recommendation.Action} Bidding:{bestBidPrice:0.00};Quantity:{quantity}");
            _logger.Verbose($"{Symbol} Buy: Checking conditions");
            _logger.Verbose($"{Symbol} Buy: {null == lastOrder} lastOrder is null");
            _logger.Verbose($"{Symbol} Buy: {lastOrder?.Side.ToString()} lastOrder side");
            _logger.Verbose($"{Symbol} Buy: {lastOrder?.Price * requiredPriceDropforRebuy > bestBidPrice} lastOrder Price: {lastOrder?.Price} * {requiredPriceDropforRebuy} > {bestBidPrice}");
            _logger.Verbose($"{Symbol} Buy: {lastOrder?.TransactionTime.AddMinutes(15) < DateTime.UtcNow} Transaction Time: {lastOrder?.TransactionTime} + 15 minutes ({lastOrder?.TransactionTime.AddMinutes(5)}) < {DateTime.UtcNow}");
            _logger.Verbose($"{Symbol} Buy: {quantity} quantity");
            _logger.Verbose($"{Symbol} Buy: {recommendation.Action} recommendation");
            _logger.Verbose($"{Symbol} Buy: {quantity * bestBidPrice >= symbolInfo.MinNotionalFilter.MinNotional} Value {quantity * bestBidPrice} > 0: {quantity * bestBidPrice > 0} and higher than minNotional {symbolInfo.MinNotionalFilter.MinNotional}");
            _logger.Verbose($"{Symbol} Buy: {symbolInfo.PriceFilter.MaxPrice >= bestBidPrice && bestBidPrice >= symbolInfo.PriceFilter.MinPrice} MaxPrice {symbolInfo.PriceFilter.MaxPrice} >= Amount {bestBidPrice} >= MinPrice {symbolInfo.PriceFilter.MinPrice}");
            _logger.Verbose($"{Symbol} Buy: {symbolInfo.LotSizeFilter.MaxQuantity >= quantity && quantity >= symbolInfo.LotSizeFilter.MinQuantity} MaxLOT {symbolInfo.LotSizeFilter.MaxQuantity} > Amount {quantity} > MinLOT {symbolInfo.LotSizeFilter.MinQuantity}");


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
            var shallFollowStrongBuy = (!_lastRecommendation.ContainsKey(recommendation.Action)
                                                        || _lastRecommendation[recommendation.Action].AddMinutes(1) < DateTime.UtcNow)
                                                    && recommendation.Action == Action.StrongBuy;

            // Check if is jump buy and last one is more than 30 seconds ago
            var isJumpBuy = recommendation.Action == Action.JumpBuy && _lastJumpBuy.AddSeconds(30) < DateTime.Now;
            if (isJumpBuy)
            {
                _lastJumpBuy = DateTime.Now;
            }


            var isLogicValid = quantity * bestBidPrice >= symbolInfo.MinNotionalFilter.MinNotional
                                && bestBidPrice > 0;

            var isPriceRangeValid = bestBidPrice >= symbolInfo.PriceFilter.MinPrice
                                        && bestBidPrice <= symbolInfo.PriceFilter.MaxPrice;

            var isLOTSizeValid = quantity >= symbolInfo.LotSizeFilter.MinQuantity
                                    && quantity <= symbolInfo.LotSizeFilter.MaxQuantity;


            _logger.Verbose($"{Symbol} Buy: {isLastOrderNull} isLastOrderNull");
            _logger.Verbose($"{Symbol} Buy: {isLastOrderSell} isLastOrderSell");
            _logger.Verbose($"{Symbol} Buy: {isLastOrderBuyAndPriceDecreased} isLastOrderBuyAndPriceDecreased");
            _logger.Verbose($"{Symbol} Buy: {isLongTimeNoRebuy} isLongTimeNoRebuy");
            _logger.Verbose($"{Symbol} Buy: {shallFollowStrongBuy} shallFollowStrongBuy");
            _logger.Verbose($"{Symbol} Buy: {shallFollowStrongBuy} shallFollowStrongBuy");
            _logger.Verbose($"{Symbol} Buy: {isJumpBuy} isJumpBuy");
            _logger.Verbose($"{Symbol} Buy: {isPriceRangeValid} isPriceRangeValid");
            _logger.Verbose($"{Symbol} Buy: {isLOTSizeValid} isLOTSizeValid");


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
                    Symbol = Symbol,
                    Side = OrderSide.Buy,
                    Type = (isJumpBuy || bestBidPrice < 50) ? OrderType.Market : OrderType.Limit,
                    OrderResponseType = OrderResponseType.Full,
                    Quantity = quantity,
                    Price = bestBidPrice,
                    TimeInForce = TimeInForce.ImmediateOrCancel,
                }, _cancellationTokenSource.Token).ConfigureAwait(true);
            }
            else
            {
                _logger.Debug($"{Symbol}: Skipping, final conditions not met");
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

            _logger.Debug($"{Symbol}: Preparing to sell");

            var assetBalance = await _accountant.GetBalance(Symbol.Replace("USDT", string.Empty)).ConfigureAwait(true);

            if (assetBalance == null || assetBalance?.Free == 0)
            {
                _logger.Debug($"{Symbol} nothing free");

                if (recommendation.Action == Datalayer.Enums.Action.PanicSell)
                {
                    _logger.Warning($"{Symbol}: PANICKING - but no asset free to sell");
                }
            }
            else
            {
                // Get best bid price
                var bestAskPrice = _buffer.GetAskPrice(Symbol);

                IQueryable<OrderTrade> buyTrades = default;
                // TODO: make scope smaller
                using (AsyncScopedLifestyle.BeginScope(Program.Container))
                {
                    var database = Program.Container.GetService<TrapeContext>();
                    try
                    {
                        // Get last orders
                        var lastOrder = database.PlacedOrders
                                            .Where(p => p.Symbol == Symbol && p.QuantityFilled > 0)
                                            .OrderByDescending(p => p.TransactionTime).AsNoTracking().FirstOrDefault();

                        // Base query
                        buyTrades = database.PlacedOrders
                                    .Where(p => p.Side == OrderSide.Buy
                                        && p.Symbol == Symbol
                                        && p.QuantityFilled > 0)
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
                        var lockedOpenOrderQuantity = _buffer.GetOpenOrderValue(Symbol);
                        quantity = lockedOpenOrderQuantity == 0 ? quantity : 0;

                        quantity = Math.Round(quantity, symbolInfo.BaseAssetPrecision);

                        if (quantity == 0)
                        {
                            _logger.Debug($"{Symbol}: Quantity is 0");
                            return;
                        }


                        // Logging
                        _logger.Debug($"{Symbol} Sell: {recommendation.Action};Asking:{bestAskPrice:0.00};Free:{assetBalance?.Free:0.00######};Quantity:{quantity:0.00######};LockedOpenOrderAmount:{lockedOpenOrderQuantity:0.00}");
                        _logger.Verbose($"{Symbol} Sell: Checking conditions");
                        _logger.Verbose($"{Symbol} Sell: {null == lastOrder} lastOrder is null");
                        _logger.Verbose($"{Symbol} Sell: {lastOrder?.Side.ToString()} lastOrder side");
                        _logger.Verbose($"{Symbol} Sell: {lastOrder?.Price * requiredPriceGainForResell < bestAskPrice} lastOrder Price: {lastOrder?.Price} * {requiredPriceGainForResell} < {bestAskPrice}");
                        _logger.Verbose($"{Symbol} Sell: {lastOrder?.TransactionTime.AddMinutes(15) < DateTime.UtcNow} Transaction Time: {lastOrder?.TransactionTime} + 15 minutes ({lastOrder?.TransactionTime.AddMinutes(5)}) < {DateTime.UtcNow}");
                        _logger.Verbose($"{Symbol} Sell: {quantity:0.00000000} quantity");
                        _logger.Verbose($"{Symbol} Sell: {recommendation.Action} recommendation");
                        _logger.Verbose($"{Symbol} Sell: {quantity * bestAskPrice >= symbolInfo.MinNotionalFilter.MinNotional} Value {quantity * bestAskPrice} > 0: {quantity * bestAskPrice > 0} and higher than {symbolInfo.MinNotionalFilter.MinNotional}");
                        _logger.Verbose($"{Symbol} Sell: {symbolInfo.PriceFilter.MaxPrice >= bestAskPrice && bestAskPrice >= symbolInfo.PriceFilter.MinPrice} MaxPrice { symbolInfo.PriceFilter.MaxPrice} > Amount {bestAskPrice} > MinPrice {symbolInfo.PriceFilter.MinPrice}");
                        _logger.Verbose($"{Symbol} Sell: {symbolInfo.LotSizeFilter.MaxQuantity >= quantity && quantity >= symbolInfo.LotSizeFilter.MinQuantity} MaxLOT {symbolInfo.LotSizeFilter.MaxQuantity} > Amount {quantity} > MinLOT {symbolInfo.LotSizeFilter.MinQuantity}");

                        // TODO: adjust logging to log only failing step and returning
                        // TODO: if-direct checks
                        // For increased readability
                        var isLastOrderNull = lastOrder == null;

                        // Check if last transaction was buy
                        var isLastOrderBuy = !isLastOrderNull && lastOrder?.Side == OrderSide.Buy
                                                                && lastOrder.TransactionTime.AddMinutes(20) < DateTime.UtcNow;

                        // Check if is panick sell but only panic once every 20 seconds
                        var isPanicking = recommendation.Action == Action.PanicSell && _lastPanicSell.AddSeconds(20) < DateTime.UtcNow;
                        if (isPanicking)
                        {
                            _lastPanicSell = DateTime.UtcNow;
                        }

                        // Check if last transaction was also sell but price has gained and transaction is older than 5 minutes
                        var isLastOrderSellAndPriceIncreased = !isLastOrderNull && lastOrder.Side == OrderSide.Sell
                                                                    && lastOrder.Price * requiredPriceGainForResell < bestAskPrice
                                                                    && lastOrder.TransactionTime.AddMinutes(7) < DateTime.UtcNow;

                        // Only one strong sell per minute
                        var shallFollowStrongSell = (!_lastRecommendation.ContainsKey(recommendation.Action)
                                                                    || _lastRecommendation[recommendation.Action].AddMinutes(5) < DateTime.UtcNow)
                                                                && recommendation.Action == Action.StrongSell;

                        // Only one panic sell per minute
                        var shallFollowPanicSell = (!_lastRecommendation.ContainsKey(recommendation.Action)
                                                                    || _lastRecommendation[recommendation.Action].AddMinutes(2) < DateTime.UtcNow)
                                                                && recommendation.Action == Action.PanicSell;

                        // Check if we just take profits
                        var isTakeProfitSell = recommendation.Action == Action.TakeProfitsSell && _lastTakeProfitSell.AddSeconds(20) < DateTime.Now;
                        if (isTakeProfitSell)
                        {
                            _lastTakeProfitSell = DateTime.UtcNow;
                        }

                        var isLogicValid = quantity * bestAskPrice >= symbolInfo.MinNotionalFilter.MinNotional
                                            && bestAskPrice > 0;

                        var isPriceRangeValid = bestAskPrice >= symbolInfo.PriceFilter.MinPrice
                                                && bestAskPrice <= symbolInfo.PriceFilter.MaxPrice;

                        var isLOTSizeValid = quantity >= symbolInfo.LotSizeFilter.MinQuantity
                                                && quantity <= symbolInfo.LotSizeFilter.MaxQuantity;

                        // Last check that amount is available
                        var isAmountAvailable = assetBalance.Free >= quantity;


                        _logger.Verbose($"{Symbol} Sell: {isLastOrderNull} isLastOrderNull");
                        _logger.Verbose($"{Symbol} Sell: {isLastOrderBuy} isLastOrderBuy");
                        _logger.Verbose($"{Symbol} Sell: {isPanicking} isPanicking");
                        _logger.Verbose($"{Symbol} Sell: {isLastOrderSellAndPriceIncreased} isLastOrderSellAndPriceIncreased");
                        _logger.Verbose($"{Symbol} Sell: {shallFollowStrongSell} shallFollowStrongSell");
                        _logger.Verbose($"{Symbol} Sell: {shallFollowPanicSell} shallFollowPanicSell");
                        _logger.Verbose($"{Symbol} Sell: {isTakeProfitSell} isTakeProfitSell");
                        _logger.Verbose($"{Symbol} Sell: {isLogicValid} isLogicValid");
                        _logger.Verbose($"{Symbol} Sell: {isPriceRangeValid} isPriceRangeValid");
                        _logger.Verbose($"{Symbol} Sell: {isLOTSizeValid} isLOTSizeValid");
                        _logger.Verbose($"{Symbol} Sell: {isAmountAvailable} isAmountAvailable");

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
                                Symbol = Symbol,
                                Side = OrderSide.Sell,
                                Type = (isTakeProfitSell || shallFollowPanicSell || bestAskPrice < 50) ? OrderType.Market : OrderType.Limit,
                                OrderResponseType = OrderResponseType.Full,
                                Quantity = quantity,
                                Price = bestAskPrice,
                                TimeInForce = TimeInForce.ImmediateOrCancel
                            }, _cancellationTokenSource.Token).ConfigureAwait(true);
                        }
                        else
                        {
                            _logger.Debug($"{Symbol}: Skipping, final conditions not met");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, e.Message);
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

            if (_jobTrading.Enabled)
            {
                _logger.Warning($"{symbol}: Broker is already active");
                return;
            }

            _logger.Information($"{symbol}: Starting Broker");

            Symbol = symbol;

            _jobTrading.Start();

            _logger.Information($"{symbol}: Broker started");
        }

        /// <summary>
        /// Terminates the broker
        /// </summary>
        /// <returns></returns>
        public async Task Terminate()
        {
            if (!_jobTrading.Enabled)
            {
                _logger.Warning($"{Symbol}: Broker is not active");
                return;
            }

            _logger.Information($"{Symbol}: Stopping Broker");

            _jobTrading.Terminate();

            // Give time for running task to end
            await Task.Delay(1000).ConfigureAwait(false);

            _cancellationTokenSource.Cancel();

            _logger.Information($"{Symbol}: Broker stopped");
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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _jobTrading.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
