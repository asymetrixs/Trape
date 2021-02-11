namespace Trape.Cli.Trader.Team
{
    using Binance.Net.Enums;
    using Binance.Net.Objects.Spot.MarketData;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Trape.Cli.Trader.Account;
    using Trape.Cli.Trader.Cache;
    using Trape.Cli.Trader.Cache.Models;
    using Trape.Cli.Trader.Market;
    using Trape.Cli.Trader.Team.Models;

    /// <summary>
    /// Does the actual trading taking into account the <c>Recommendation</c> of the <c>Analyst</c> and previous trades
    /// </summary>
    public class Broker : IBroker, IDisposable, IStartable
    {
        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Records last strongs to not execute a strong <c>Recommendation</c> one after the other without a break inbetween
        /// </summary>
        private readonly Dictionary<ActionType, DateTime> _lastRecommendation;

        /// <summary>
        /// Semaphore synchronizes access in case a task takes longer to return and the timer would elapse again
        /// </summary>
        private readonly SemaphoreSlim _canTrade;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Accountant
        /// </summary>
        private readonly IAccountant _accountant;

        /// <summary>
        /// Cache
        /// </summary>
        private readonly IStore _cache;

        /// <summary>
        /// Stock Exchange
        /// </summary>
        private readonly IStockExchange _stockExchange;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Analyst subscriber
        /// </summary>
        private IDisposable? _analystSubscriber;

        /// <summary>
        /// Initializes a new instance of the <c>Broker</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="cache">Cache</param>
        /// <param name="stockExchange">Stock Exchange</param>
        public Broker(ILogger logger, IAccountant accountant, IStore cache, IStockExchange stockExchange)
        {
            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            this._cache = cache ?? throw new ArgumentNullException(paramName: nameof(cache));

            this._stockExchange = stockExchange ?? throw new ArgumentNullException(paramName: nameof(stockExchange));

            this._logger = logger.ForContext<Broker>();
            this._cancellationTokenSource = new CancellationTokenSource();
            this._lastRecommendation = new Dictionary<ActionType, DateTime>();
            this._canTrade = new SemaphoreSlim(1, 1);
            this.Symbol = null;
        }

        /// <summary>
        /// Base Asset
        /// </summary>
        public string BaseAsset => this.Symbol is null ? string.Empty : this.Symbol.BaseAsset;

        /// <summary>
        /// Last time <c>Broker</c> was active
        /// </summary>
        public DateTime LastActive { get; private set; }

        /// <summary>
        /// Symbol
        /// </summary>
        public BinanceSymbol? Symbol { get; private set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name => this.Symbol is null ? string.Empty : this.Symbol.Name;

        /// <summary>
        /// Is Faulty
        /// </summary>
        public bool IsFaulty { get; }

        /// <summary>
        /// Links this broker to an analyst
        /// </summary>
        /// <param name="analyst">Analyst</param>
        public void SubscribeTo(IAnalyst analyst)
        {
            this._analystSubscriber = analyst?.NewRecommendation.Subscribe(this.Trade);
        }

        /// <summary>
        /// Start the Broker
        /// </summary>
        /// <param name="symbol">Symbol to trade</param>
        public Task Start(BinanceSymbol symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(paramName: nameof(symbol));

            this._logger.Information($"{symbol}: Starting Broker");

            this.Symbol = symbol;

            this._logger.Information($"{symbol}: Broker started");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Terminates the broker
        /// </summary>
        /// <returns></returns>
        public async Task Terminate()
        {
            this._logger.Information($"{this.BaseAsset}: Stopping Broker");

            // Give time for running task to end
            await Task.Delay(1000).ConfigureAwait(false);

            this._cancellationTokenSource.Cancel();

            this._logger.Information($"{this.BaseAsset}: Broker stopped");
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._cancellationTokenSource.Dispose();
                this._canTrade.Dispose();
                this._analystSubscriber?.Dispose();
            }

            this._disposed = true;
        }

        /// <summary>
        /// Checks the <c>Recommendation</c> of the <c>Analyst</c> and previous trades and decides whether to buy, wait or sell
        /// </summary>
        /// <param name="recommendation">Recommmendation</param>
        private async void Trade(Recommendation recommendation)
        {
            this.LastActive = DateTime.UtcNow;

            if (recommendation.Action == ActionType.Hold)
            {
                Console.WriteLine($"{this.BaseAsset}: Recommendation is hold.");
                this._logger.Verbose($"{this.BaseAsset}: Recommendation is hold.");
                return;
            }

            // If this point is reached, the preconditions are fine, check if slot is available
            if (this._canTrade.CurrentCount == 0)
            {
                return;
            }

            // Check if there is an open order
            if (this.Symbol is null || this._cache.HasOpenOrder(this.Symbol.Name))
            {
                return;
            }

            try
            {
                // Wait because context is available, otherwise would have exited before reaching this step
                // Synchronizing is just used in the rare case that a method needs longer to return than the timer needs to elapse
                // so that no other task runs the same method
                this._canTrade.Wait();

                if (recommendation.Action == ActionType.Buy)
                {
                    await this.Buy(recommendation).ConfigureAwait(true);
                }
                else if (recommendation.Action == ActionType.Sell)
                {
                    await this.Sell(recommendation).ConfigureAwait(true);
                }

                // Log recommended action, use for strong recommendations
                this._lastRecommendation[recommendation.Action] = DateTime.UtcNow;
            }
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

        /// <summary>
        /// Runs some checks and places an order if possible
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <returns>Placed order or null if none placed</returns>
        private async Task Buy(Recommendation recommendation)
        {
            if (this.Symbol is null)
            {
                return;
            }

            this._logger.Debug($"{this.BaseAsset}: Preparing to buy");

            // Get remaining USDT balance for trading
            var usdt = await this._accountant.GetBalance("USDT").ConfigureAwait(false);
            if (usdt == null)
            {
                // Something is oddly wrong, wait a bit
                this._logger.Debug("Cannot retrieve account info");
                return;
            }

            // Check if it was not bought before
            var assetBalance = await this._accountant.GetBalance(this.BaseAsset).ConfigureAwait(true);
            if (assetBalance?.Total > 0)
            {
                this._logger.Debug($"{this.BaseAsset}: Asset already in stock");
                return;
            }

            // Max 20 USDT or what is available
            var buyForUSDT = usdt.Free < 20 ? usdt.Free : 20;

            // Round to a valid value
            //  symbolInfo.BaseAssetPrecision, MidpointRounding.ToZero
            buyForUSDT = Math.Round(buyForUSDT, 2, MidpointRounding.ToZero);

            if (buyForUSDT == 0)
            {
                this._logger.Debug($"{this.BaseAsset}: No funds available");
                return;
            }

            // Get ask price
            var bestBidPrice = recommendation.BestAskPrice;
            var quantity = buyForUSDT / bestBidPrice;
            quantity = Math.Round(quantity, this.Symbol.BaseAssetPrecision);

            if (quantity == 0)
            {
                this._logger.Debug($"{this.BaseAsset}: Quantity is 0");
                return;
            }

            // Logging
            this._logger.Debug($"{this.BaseAsset} Buy: {recommendation.Action} Bidding:{bestBidPrice:0.00};Quantity:{quantity}");
            this._logger.Verbose($"{this.BaseAsset} Buy: Checking conditions");
            this._logger.Verbose($"{this.BaseAsset} Buy: {quantity} quantity");
            this._logger.Verbose($"{this.BaseAsset} Buy: {recommendation.Action} recommendation");
            this._logger.Verbose($"{this.BaseAsset} Buy: {quantity * bestBidPrice >= this.Symbol.MinNotionalFilter?.MinNotional} Value {quantity * bestBidPrice} > 0: {quantity * bestBidPrice > 0} and higher than minNotional {this.Symbol.MinNotionalFilter?.MinNotional}");
            this._logger.Verbose($"{this.BaseAsset} Buy: {this.Symbol.PriceFilter?.MaxPrice >= bestBidPrice && bestBidPrice >= this.Symbol.PriceFilter.MinPrice} MaxPrice {this.Symbol.PriceFilter?.MaxPrice} >= Amount {bestBidPrice} >= MinPrice {this.Symbol.PriceFilter?.MinPrice}");
            this._logger.Verbose($"{this.BaseAsset} Buy: {this.Symbol.LotSizeFilter?.MaxQuantity >= quantity && quantity >= this.Symbol.LotSizeFilter.MinQuantity} MaxLOT {this.Symbol.LotSizeFilter?.MaxQuantity} > Amount {quantity} > MinLOT {this.Symbol.LotSizeFilter?.MinQuantity}");

            var isLogicValid = quantity * bestBidPrice >= this.Symbol.MinNotionalFilter?.MinNotional
                                && bestBidPrice > 0;

            var isPriceRangeValid = bestBidPrice >= this.Symbol.PriceFilter?.MinPrice
                                        && bestBidPrice <= this.Symbol.PriceFilter.MaxPrice;

            var isLOTSizeValid = quantity >= this.Symbol.LotSizeFilter?.MinQuantity
                                    && quantity <= this.Symbol.LotSizeFilter.MaxQuantity;

            this._logger.Verbose($"{this.BaseAsset} Buy: {isLogicValid}       isLogicValid");
            this._logger.Verbose($"{this.BaseAsset} Buy: {isPriceRangeValid}  isPriceRangeValid");
            this._logger.Verbose($"{this.BaseAsset} Buy: {isLOTSizeValid}     isLOTSizeValid");

            // Check conditions for buy
            if (isLogicValid && isPriceRangeValid && isLOTSizeValid)
            {
                // Get stock exchange and place order
                await this._stockExchange.PlaceOrder(new ClientOrder(this.Symbol.Name)
                {
                    Side = OrderSide.Buy,
                    Type = OrderType.Market,
                    OrderResponseType = OrderResponseType.Full,
                    Quantity = quantity,
                    Price = bestBidPrice,
                    TimeInForce = TimeInForce.ImmediateOrCancel,
                }, this._cancellationTokenSource.Token).ConfigureAwait(true);
            }
            else
            {
                this._logger.Debug($"{this.BaseAsset}: Skipping, final conditions not met");
            }
        }

        /// <summary>
        /// Runs some checks and places an order if possible
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <returns>Placed order or null if none placed</returns>
        private async Task Sell(Recommendation recommendation)
        {
            if (this.Symbol is null)
            {
                return;
            }

            this._logger.Debug($"{this.BaseAsset}: Preparing to sell");

            // Check if asset can be sold
            var assetBalance = await this._accountant.GetBalance(this.BaseAsset).ConfigureAwait(true);
            if (assetBalance is null || assetBalance.Free == 0)
            {
                this._logger.Debug($"{this.BaseAsset}: Nothing free");
                return;
            }

            // Get best bid price
            var bestAskPrice = recommendation.BestAskPrice;

            var quantity = assetBalance.Free;

            // Wait until previous trades were handled
            var lockedOpenOrderQuantity = this._cache.GetOpenOrderValue(this.BaseAsset);
            quantity = lockedOpenOrderQuantity == 0 ? quantity : 0;

            quantity = Math.Round(quantity, this.Symbol.BaseAssetPrecision);

            if (quantity == 0)
            {
                this._logger.Debug($"{this.BaseAsset}: Quantity is 0");
                return;
            }

            // Logging
            this._logger.Debug($"{this.BaseAsset} Sell: {recommendation.Action};Asking:{bestAskPrice:0.00};Free:{assetBalance.Free:0.00######};Quantity:{quantity:0.00######};LockedOpenOrderAmount:{lockedOpenOrderQuantity:0.00}");
            this._logger.Verbose($"{this.BaseAsset} Sell: Checking conditions");
            this._logger.Verbose($"{this.BaseAsset} Sell:  lastOrder is null");
            this._logger.Verbose($"{this.BaseAsset} Sell: {quantity:0.00000000} quantity");
            this._logger.Verbose($"{this.BaseAsset} Sell: {recommendation.Action} recommendation");
            this._logger.Verbose($"{this.BaseAsset} Sell: {quantity * bestAskPrice >= this.Symbol.MinNotionalFilter?.MinNotional} Value {quantity * bestAskPrice} > 0: {quantity * bestAskPrice > 0} and higher than {this.Symbol.MinNotionalFilter?.MinNotional}");
            this._logger.Verbose($"{this.BaseAsset} Sell: {this.Symbol.PriceFilter?.MaxPrice >= bestAskPrice && bestAskPrice >= this.Symbol.PriceFilter.MinPrice} MaxPrice {this.Symbol.PriceFilter?.MaxPrice} > Amount {bestAskPrice} > MinPrice {this.Symbol.PriceFilter?.MinPrice}");
            this._logger.Verbose($"{this.BaseAsset} Sell: {this.Symbol.LotSizeFilter?.MaxQuantity >= quantity && quantity >= this.Symbol.LotSizeFilter.MinQuantity} MaxLOT {this.Symbol.LotSizeFilter?.MaxQuantity} > Amount {quantity} > MinLOT {this.Symbol.LotSizeFilter?.MinQuantity}");

            var isLogicValid = quantity * bestAskPrice >= this.Symbol.MinNotionalFilter?.MinNotional
                                && bestAskPrice > 0;

            var isPriceRangeValid = bestAskPrice >= this.Symbol.PriceFilter?.MinPrice
                                    && bestAskPrice <= this.Symbol.PriceFilter.MaxPrice;

            var isLOTSizeValid = quantity >= this.Symbol.LotSizeFilter?.MinQuantity
                                    && quantity <= this.Symbol.LotSizeFilter.MaxQuantity;

            // Last check that amount is available
            var isAmountAvailable = assetBalance.Free >= quantity;

            this._logger.Verbose($"{this.BaseAsset} Sell: {isLogicValid}      isLogicValid");
            this._logger.Verbose($"{this.BaseAsset} Sell: {isPriceRangeValid} isPriceRangeValid");
            this._logger.Verbose($"{this.BaseAsset} Sell: {isLOTSizeValid}    isLOTSizeValid");
            this._logger.Verbose($"{this.BaseAsset} Sell: {isAmountAvailable} isAmountAvailable");

            // Check conditions for sell
            if (isLogicValid && isPriceRangeValid && isLOTSizeValid && isAmountAvailable)
            {
                // Get stock exchange and place order
                await this._stockExchange.PlaceOrder(new ClientOrder(this.Symbol.Name)
                {
                    Side = OrderSide.Sell,
                    Type = OrderType.Market,
                    OrderResponseType = OrderResponseType.Full,
                    Quantity = quantity,
                    Price = bestAskPrice,
                    TimeInForce = TimeInForce.ImmediateOrCancel,
                }, this._cancellationTokenSource.Token).ConfigureAwait(true);
            }
            else
            {
                this._logger.Debug($"{this.BaseAsset}: Skipping, final conditions not met");
            }
        }
    }
}
