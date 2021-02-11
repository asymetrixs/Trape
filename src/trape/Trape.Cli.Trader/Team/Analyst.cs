namespace Trape.Cli.Trader.Team
{
    using Binance.Net.Interfaces;
    using Binance.Net.Objects.Spot.MarketData;
    using Binance.Net.Objects.Spot.MarketStream;
    using CryptoExchange.Net.Sockets;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using Trape.Cli.Trader.Account;
    using Trape.Cli.Trader.Listener;
    using Trape.Cli.Trader.Team.Models;
    using Trape.Jobs;

    /// <summary>
    /// This class represents an analyst. It's task is to make recommendations on
    /// buying, keeping (wait), or selling assets based on different facts (slope, moving average, etc.)
    /// </summary>
    public class Analyst : IAnalyst, IDisposable, IStartable
    {
        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Accountant
        /// </summary>
        private readonly IAccountant _accountant;

        /// <summary>
        /// Binance Socket Client
        /// </summary>
        private readonly IBinanceSocketClient _binanceSocketClient;

        /// <summary>
        /// Cancellation Token
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Timer when Analyst makes a new decision
        /// </summary>
        private readonly Job _jobRecommender;

        /// <summary>
        /// Recommendations
        /// </summary>
        private readonly Subject<Recommendation> _newRecommendation;

        /// <summary>
        /// Current prices
        /// </summary>
        private readonly ConcurrentQueue<CurrentBookPrice> _currentPrices;

        /// <summary>
        /// Thresholds
        /// </summary>
        private readonly Thresholds _thresholds;

        /// <summary>
        /// Best bid price
        /// </summary>
        private readonly BestPrice _bestBidPrice;

        /// <summary>
        /// Best ask price
        /// </summary>
        private readonly BestPrice _bestAskPrice;

        /// <summary>
        /// Subscriptions
        /// </summary>
        private UpdateSubscription? _subscription;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Constructs a new instance of the <c>Analyst</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public Analyst(ILogger logger, IAccountant accountant, IBinanceSocketClient binanceSocketClient)
        {
            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            this._logger = logger.ForContext<Analyst>();
            this._binanceSocketClient = binanceSocketClient;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._newRecommendation = new Subject<Recommendation>();
            this._currentPrices = new ConcurrentQueue<CurrentBookPrice>();
            this._bestBidPrice = new BestPrice();
            this._bestAskPrice = new BestPrice();
            this.IsFaulty = false;
            this._disposed = false;
            this._thresholds = new Thresholds();
            this.Symbol = null;

            // Set up timer that makes decisions, every second
            this._jobRecommender = new Job(new TimeSpan(0, 0, 0, 0, 100), this.Recommending, this._cancellationTokenSource.Token);
        }

        /// <summary>
        /// Recommendations
        /// </summary>
        public IObservable<Recommendation> NewRecommendation => this._newRecommendation;

        /// <summary>
        /// Symbol
        /// </summary>
        public string Name => this.Symbol is null ? string.Empty : this.Symbol.Name;

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
        /// Faulty
        /// </summary>
        public bool IsFaulty { get; private set; }

        /// <summary>
        /// Starts the <c>Analyst</c> instance
        /// </summary>
        public async Task Start(BinanceSymbol symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(paramName: nameof(symbol));

            if (this._jobRecommender.Enabled)
            {
                this._logger.Warning($"{symbol}: Analyst is already active");
                return;
            }

            this._logger.Information($"{this.BaseAsset}: Starting Analyst");

            this.Symbol = symbol;

            // Subscribe to symbol
            var result = await this._binanceSocketClient.Spot.SubscribeToBookTickerUpdatesAsync(this.Symbol.Name,
                async (BinanceStreamBookPrice bsbp) =>
                {
                    this._currentPrices.Enqueue(new CurrentBookPrice(bsbp));

                    // Record rise
                    Task hitPrice = this._thresholds.Hit(bsbp.BestAskPrice);

                    // Record asking and bidding price
                    Task addBestAskPrice = this._bestAskPrice.Add(bsbp.BestAskPrice);
                    Task addBestBidPrice = this._bestBidPrice.Add(bsbp.BestBidPrice);

                    await Task.WhenAll(hitPrice, addBestAskPrice, addBestBidPrice).ConfigureAwait(true);

                    this._logger.Verbose($"{bsbp.Symbol}: Book tick update - asking is {bsbp.BestAskPrice:0.00}");
                    this._logger.Verbose($"{bsbp.Symbol}: Book tick update - bidding is {bsbp.BestBidPrice:0.00}");
                }).ConfigureAwait(true);

            if (!result.Success)
            {
                this.IsFaulty = true;
                this._logger.Warning($"{this.BaseAsset}: Analyst is FAULTY");
            }
            else
            {
                this._subscription = result.Data;

                // Do not start recommender when state is faulty
                this._jobRecommender.Start();
                this._logger.Information($"{this.BaseAsset}: Analyst started");
            }
        }

        /// <summary>
        /// Stops the <c>Analyst</c> instance
        /// </summary>
        public async Task Terminate()
        {
            // Stop recommendation maker
            this._jobRecommender.Terminate();

            this._newRecommendation.OnCompleted();

            // Terminate possible running tasks
            this._cancellationTokenSource.Cancel();

            if (this._subscription is not null)
            {
                await this._binanceSocketClient.Unsubscribe(this._subscription).ConfigureAwait(true);
            }

            this._logger.Information("Analyst stopped");

            await Task.CompletedTask.ConfigureAwait(false);
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
                this._jobRecommender.Dispose();
                this._newRecommendation.Dispose();
                this._bestAskPrice.Dispose();
                this._bestBidPrice.Dispose();
                this._thresholds.Dispose();
            }

            this._disposed = true;
        }

        /// <summary>
        /// Create a new decision for this symbol
        /// </summary>
        private async void Recommending()
        {
            if (this.Symbol is null)
            {
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            this.LastActive = DateTime.UtcNow;

            // First thing is to issue a buy if this asset it not in stock yet
            var stockAmount = await this._accountant.GetBalance(this.BaseAsset).ConfigureAwait(true);

            if (stockAmount is null || stockAmount.Total == 0)
            {
                // Check if price is valid
                var latestAskPrice = this._bestAskPrice.Latest;
                var latestBidPrice = this._bestBidPrice.Latest;

                if (latestAskPrice is null || latestBidPrice is null)
                {
                    this._logger.Verbose($"{this.Symbol.Name}: No price available");
                    return;
                }

                // Recommend buy
                this._newRecommendation.OnNext(new Recommendation()
                {
                    Action = ActionType.Buy,
                    BestAskPrice = latestAskPrice.Value,
                    BestBidPrice = latestBidPrice.Value,
                });

                this._logger.Information($"{this.Symbol.Name}: Recommended to buy");

                return;
            }

            // Use regular approach
            // Get current symbol price
            var currentPrice = this._bestBidPrice.GetAverage();

            if (currentPrice < 0)
            {
                this._logger.Verbose($"Skipped {this.Symbol.Name} due to old or incomplete data: {currentPrice:0.00}");

                return;
            }

            // Trade summary
            var tradeSummary = this._accountant.GetTradeSummary(this.Symbol.Name);
            if (tradeSummary is null)
            {
                this._logger.Verbose($"{this.Symbol.Name}: No Trade Summary available");
                return;
            }

            this._logger.Debug($"{this.Symbol.Name}: Trade Summary {tradeSummary.Quantity} @ {tradeSummary.PricePerUnit} (eff. {tradeSummary.QuoteQuantity,2}");

            // Make the decision
            var action = ActionType.Hold;

            if (this._thresholds.IsDroppingBelowThreshold(currentPrice))
            {
                action = ActionType.Sell;
            }

            if (action != ActionType.Hold)
            {
                this._logger.Debug($"{this.Symbol.Name}: {action} @ {currentPrice}");

                // Instantiate new recommendation
                this._newRecommendation.OnNext(new Recommendation()
                {
                    Action = action,
                    Price = currentPrice,
                    BestAskPrice = this._bestAskPrice.GetAverage(),
                    BestBidPrice = this._bestBidPrice.GetAverage(),
                });
            }

            stopwatch.Stop();

            this._logger.Debug($"{this.Symbol.Name}: Recommendation time: {stopwatch.Elapsed.TotalMilliseconds}");
        }
    }
}
