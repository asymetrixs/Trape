using Binance.Net.Objects.Spot.MarketData;
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
using Trape.Cli.trader.Listener;
using Trape.Cli.trader.Market;
using Trape.Cli.trader.Team;
using Trape.Datalayer.Models;
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
        private readonly IListener _buffer;

        /// <summary>
        /// Minimum required increase of the price before another chunk is sold
        /// </summary>
        private const decimal requiredPriceGainForResell = 1.003M;

        /// <summary>
        /// Minimum required decrease of the price before another chunk is bought
        /// </summary>
        private const decimal requiredPriceDropforRebuy = 0.997M;

        /// <summary>
        /// Base Asset
        /// </summary>
        public string BaseAsset => Symbol.BaseAsset;

        /// <summary>
        /// Last time <c>Broker</c> was active
        /// </summary>
        public DateTime LastActive { get; private set; }

        /// <summary>
        /// Symbol
        /// </summary>
        public BinanceSymbol Symbol { get; private set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name => Symbol.Name;

        /// <summary>
        /// Is Faulty
        /// </summary>
        public bool IsFaulty { get; private set; }

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

        /// <summary>
        /// Analyst subscriber
        /// </summary>
        private IDisposable _analystSubscriber;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Broker</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="buffer">Buffer</param>
        public Broker(ILogger logger, IAccountant accountant, IListener buffer)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            _buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            _logger = logger.ForContext<Broker>();
            _cancellationTokenSource = new CancellationTokenSource();
            _lastRecommendation = new Dictionary<Action, DateTime>();
            _canTrade = new SemaphoreSlim(1, 1);
            _lastPanicSell = default;
            _lastTakeProfitSell = default;
            _lastJumpBuy = default;
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Checks the <c>Recommendation</c> of the <c>Analyst</c> and previous trades and decides whether to buy, wait or sell
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Trade(Recommendation recommendation)
        {
            LastActive = DateTime.UtcNow;

            if (recommendation.Action == Datalayer.Enums.Action.Hold)
            {
                Console.WriteLine($"{BaseAsset}: Recommendation is hold.");
                _logger.Verbose($"{BaseAsset}: Recommendation is hold.");
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

                // Generate new client order id
                var newClientOrderId = Guid.NewGuid().ToString("N");

                // Buy
                if (recommendation.Action == Action.Buy
                || recommendation.Action == Action.StrongBuy
                || recommendation.Action == Action.JumpBuy)
                {
                    // Buy
                    await Buy(recommendation).ConfigureAwait(true);
                }
                // Sell
                else if (recommendation.Action == Action.Sell
                    || recommendation.Action == Action.StrongSell
                    || recommendation.Action == Action.PanicSell
                    || recommendation.Action == Action.TakeProfitsSell
                    || recommendation.Action == Action.Hold)
                {
                    await Sell(recommendation).ConfigureAwait(true);
                }

                // Log recommended action, use for strong recommendations
                _lastRecommendation[recommendation.Action] = DateTime.UtcNow;
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
        /// Links this broker to an analyst
        /// </summary>
        /// <param name="analyst"></param>
        public void SubscribeTo(IAnalyst analyst)
        {
            _analystSubscriber = analyst.NewRecommendation.Subscribe(Trade);
        }

        /// <summary>
        /// Runs some checks and places an order if possible
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <param name="symbolInfo">Symbol information</param>
        /// <returns>Placed order or null if none placed</returns>
        public async Task Buy(Recommendation recommendation)
        {
            _logger.Debug($"{BaseAsset}: Preparing to buy");

            // Get remaining USDT balance for trading
            var usdt = await _accountant.GetBalance("USDT").ConfigureAwait(false);
            if (usdt == null)
            {
                // Something is oddly wrong, wait a bit
                _logger.Debug("Cannot retrieve account info");
                return;
            }

            // Check if it was not bought before
            var assetBalance = await _accountant.GetBalance(BaseAsset).ConfigureAwait(true);

            if (assetBalance.Total > 0)
            {
                _logger.Debug($"{BaseAsset}: Asset already in stock");
            }

            // Max 20 USDT or what is available 
            var buyForUSDT = 20 > usdt.Free ? usdt.Free : 20;

            // Round to a valid value
            //  symbolInfo.BaseAssetPrecision, MidpointRounding.ToZero
            buyForUSDT = Math.Round(buyForUSDT, 2, MidpointRounding.ToZero);

            if (buyForUSDT == 0)
            {
                _logger.Debug($"{BaseAsset}: No funds available");
                return;
            }

            // Get ask price
            var bestBidPrice = _buffer.GetBidPrice(BaseAsset);

            var quantity = buyForUSDT / bestBidPrice;

            quantity = Math.Round(quantity, Symbol.BaseAssetPrecision);

            if (quantity == 0)
            {
                _logger.Debug($"{BaseAsset}: Quantity is 0");
                return;
            }

            // Logging
            _logger.Debug($"{BaseAsset} Buy: {recommendation.Action} Bidding:{bestBidPrice:0.00};Quantity:{quantity}");
            _logger.Verbose($"{BaseAsset} Buy: Checking conditions");
            _logger.Verbose($"{BaseAsset} Buy: {quantity} quantity");
            _logger.Verbose($"{BaseAsset} Buy: {recommendation.Action} recommendation");
            _logger.Verbose($"{BaseAsset} Buy: {quantity * bestBidPrice >= Symbol.MinNotionalFilter.MinNotional} Value {quantity * bestBidPrice} > 0: {quantity * bestBidPrice > 0} and higher than minNotional {Symbol.MinNotionalFilter.MinNotional}");
            _logger.Verbose($"{BaseAsset} Buy: {Symbol.PriceFilter.MaxPrice >= bestBidPrice && bestBidPrice >= Symbol.PriceFilter.MinPrice} MaxPrice {Symbol.PriceFilter.MaxPrice} >= Amount {bestBidPrice} >= MinPrice {Symbol.PriceFilter.MinPrice}");
            _logger.Verbose($"{BaseAsset} Buy: {Symbol.LotSizeFilter.MaxQuantity >= quantity && quantity >= Symbol.LotSizeFilter.MinQuantity} MaxLOT {Symbol.LotSizeFilter.MaxQuantity} > Amount {quantity} > MinLOT {Symbol.LotSizeFilter.MinQuantity}");

            var isLogicValid = quantity * bestBidPrice >= Symbol.MinNotionalFilter.MinNotional
                                && bestBidPrice > 0;

            var isPriceRangeValid = bestBidPrice >= Symbol.PriceFilter.MinPrice
                                        && bestBidPrice <= Symbol.PriceFilter.MaxPrice;

            var isLOTSizeValid = quantity >= Symbol.LotSizeFilter.MinQuantity
                                    && quantity <= Symbol.LotSizeFilter.MaxQuantity;

            _logger.Verbose($"{BaseAsset} Buy: {isLogicValid}       isLogicValid");
            _logger.Verbose($"{BaseAsset} Buy: {isPriceRangeValid}  isPriceRangeValid");
            _logger.Verbose($"{BaseAsset} Buy: {isLOTSizeValid}     isLOTSizeValid");

            // Check conditions for buy
            if (
                    // Logic check
                    isLogicValid
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
                    Symbol = BaseAsset,
                    Side = OrderSide.Buy,
                    Type = OrderType.Market,
                    OrderResponseType = OrderResponseType.Full,
                    Quantity = quantity,
                    Price = bestBidPrice,
                    TimeInForce = TimeInForce.ImmediateOrCancel,
                }, _cancellationTokenSource.Token).ConfigureAwait(true);
            }
            else
            {
                _logger.Debug($"{BaseAsset}: Skipping, final conditions not met");
            }
        }

        /// <summary>
        /// Runs some checks and places an order if possible
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <param name="symbolInfo">Symbol info</param>
        /// <returns>Placed order or null if none placed</returns>
        public async Task Sell(Recommendation recommendation)
        {
            _logger.Debug($"{BaseAsset}: Preparing to sell");

            var assetBalance = await _accountant.GetBalance(BaseAsset).ConfigureAwait(true);

            if (assetBalance == null || assetBalance?.Free == 0)
            {
                _logger.Debug($"{BaseAsset} nothing free");

                if (recommendation.Action == Datalayer.Enums.Action.PanicSell)
                {
                    _logger.Warning($"{BaseAsset}: PANICKING - but no asset free to sell");
                }
            }
            else
            {
                // Get best bid price
                var bestAskPrice = _buffer.GetAskPrice(BaseAsset);

                IQueryable<OrderTrade> buyTrades = default;
                // TODO: make scope smaller
                using (AsyncScopedLifestyle.BeginScope(Program.Container))
                {
                    try
                    {
                        var quantity = assetBalance.Free;

                        // Wait until previous trades were handled
                        var lockedOpenOrderQuantity = _buffer.GetOpenOrderValue(BaseAsset);
                        quantity = lockedOpenOrderQuantity == 0 ? quantity : 0;

                        quantity = Math.Round(quantity, Symbol.BaseAssetPrecision);

                        if (quantity == 0)
                        {
                            _logger.Debug($"{BaseAsset}: Quantity is 0");
                            return;
                        }

                        // Logging
                        _logger.Debug($"{BaseAsset} Sell: {recommendation.Action};Asking:{bestAskPrice:0.00};Free:{assetBalance?.Free:0.00######};Quantity:{quantity:0.00######};LockedOpenOrderAmount:{lockedOpenOrderQuantity:0.00}");
                        _logger.Verbose($"{BaseAsset} Sell: Checking conditions");
                        _logger.Verbose($"{BaseAsset} Sell:  lastOrder is null");
                        _logger.Verbose($"{BaseAsset} Sell: {quantity:0.00000000} quantity");
                        _logger.Verbose($"{BaseAsset} Sell: {recommendation.Action} recommendation");
                        _logger.Verbose($"{BaseAsset} Sell: {quantity * bestAskPrice >= Symbol.MinNotionalFilter.MinNotional} Value {quantity * bestAskPrice} > 0: {quantity * bestAskPrice > 0} and higher than {Symbol.MinNotionalFilter.MinNotional}");
                        _logger.Verbose($"{BaseAsset} Sell: {Symbol.PriceFilter.MaxPrice >= bestAskPrice && bestAskPrice >= Symbol.PriceFilter.MinPrice} MaxPrice {Symbol.PriceFilter.MaxPrice} > Amount {bestAskPrice} > MinPrice {Symbol.PriceFilter.MinPrice}");
                        _logger.Verbose($"{BaseAsset} Sell: {Symbol.LotSizeFilter.MaxQuantity >= quantity && quantity >= Symbol.LotSizeFilter.MinQuantity} MaxLOT {Symbol.LotSizeFilter.MaxQuantity} > Amount {quantity} > MinLOT {Symbol.LotSizeFilter.MinQuantity}");

                        // TODO: adjust logging to log only failing step and returning
                        // TODO: if-direct checks

                        // Check if is panick sell but only panic once every 20 seconds
                        if (recommendation.Action == Action.PanicSell && _lastPanicSell.AddSeconds(20) < DateTime.UtcNow)
                        {
                            _lastPanicSell = DateTime.UtcNow;
                        }


                        // Check if we just take profits
                        if (recommendation.Action == Action.TakeProfitsSell && _lastTakeProfitSell.AddSeconds(20) < DateTime.Now)
                        {
                            _lastTakeProfitSell = DateTime.UtcNow;
                        }

                        var isLogicValid = quantity * bestAskPrice >= Symbol.MinNotionalFilter.MinNotional
                                            && bestAskPrice > 0;

                        var isPriceRangeValid = bestAskPrice >= Symbol.PriceFilter.MinPrice
                                                && bestAskPrice <= Symbol.PriceFilter.MaxPrice;

                        var isLOTSizeValid = quantity >= Symbol.LotSizeFilter.MinQuantity
                                                && quantity <= Symbol.LotSizeFilter.MaxQuantity;

                        // Last check that amount is available
                        var isAmountAvailable = assetBalance.Free >= quantity;

                        _logger.Verbose($"{BaseAsset} Sell: {isLogicValid}      isLogicValid");
                        _logger.Verbose($"{BaseAsset} Sell: {isPriceRangeValid} isPriceRangeValid");
                        _logger.Verbose($"{BaseAsset} Sell: {isLOTSizeValid}    isLOTSizeValid");
                        _logger.Verbose($"{BaseAsset} Sell: {isAmountAvailable} isAmountAvailable");

                        // Check conditions for sell
                        if (
                                // Logic check
                                isLogicValid
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
                                Symbol = BaseAsset,
                                Side = OrderSide.Sell,
                                Type = OrderType.Market,
                                OrderResponseType = OrderResponseType.Full,
                                Quantity = quantity,
                                Price = bestAskPrice,
                                TimeInForce = TimeInForce.ImmediateOrCancel
                            }, _cancellationTokenSource.Token).ConfigureAwait(true);
                        }
                        else
                        {
                            _logger.Debug($"{BaseAsset}: Skipping, final conditions not met");
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
        public Task Start(BinanceSymbol symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(paramName: nameof(symbol));

            _logger.Information($"{symbol}: Starting Broker");

            Symbol = symbol;

            _logger.Information($"{symbol}: Broker started");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Terminates the broker
        /// </summary>
        /// <returns></returns>
        public async Task Terminate()
        {
            _logger.Information($"{BaseAsset}: Stopping Broker");

            // Give time for running task to end
            await Task.Delay(1000).ConfigureAwait(false);

            _cancellationTokenSource.Cancel();

            _logger.Information($"{BaseAsset}: Broker stopped");
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

            }

            _disposed = true;
        }

        #endregion
    }
}
