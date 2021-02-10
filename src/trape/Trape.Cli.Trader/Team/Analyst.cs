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

namespace Trape.Cli.Trader.Team
{
    /// <summary>
    /// This class represents an analyst. It's task is to make recommendations on
    /// buying, keeping (wait), or selling assets based on different facts (slope, moving average, etc.)
    /// </summary>
    public class Analyst : IAnalyst, IDisposable, IStartable
    {
        #region Fields

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
        /// Subscriptions
        /// </summary>
        private UpdateSubscription? _subscription;

        /// <summary>
        /// Timer when Analyst makes a new decision
        /// </summary>
        private readonly Job _jobRecommender;

        /// <summary>
        /// Thresholds
        /// </summary>
        private Thresholds? _thresholds;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Recommendations
        /// </summary>
        private readonly Subject<Recommendation> _newRecommendation;

        /// <summary>
        /// Current prices
        /// </summary>
        private readonly ConcurrentQueue<CurrentBookPrice> _currentPrices;

        /// <summary>
        /// Best bid price
        /// </summary>
        private readonly BestPrice _bestBidPrice;

        /// <summary>
        /// Best ask price
        /// </summary>
        private readonly BestPrice _bestAskPrice;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new instance of the <c>Analyst</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public Analyst(ILogger logger, IAccountant accountant, IBinanceSocketClient binanceSocketClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            #endregion

            _logger = logger.ForContext<Analyst>();
            _binanceSocketClient = binanceSocketClient;
            _cancellationTokenSource = new CancellationTokenSource();
            _newRecommendation = new Subject<Recommendation>();
            _currentPrices = new ConcurrentQueue<CurrentBookPrice>();
            _bestBidPrice = new BestPrice();
            _bestAskPrice = new BestPrice();
            IsFaulty = false;
            _disposed = false;

            // Set up timer that makes decisions, every second
            _jobRecommender = new Job(new TimeSpan(0, 0, 0, 0, 100), Recommending, _cancellationTokenSource.Token);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Recommendations
        /// </summary>
        public IObservable<Recommendation> NewRecommendation => _newRecommendation;

        /// <summary>
        /// Symbol
        /// </summary>
        public string Name => Symbol.Name;

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
        /// Faulty
        /// </summary>
        public bool IsFaulty { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Create a new decision for this symbol
        /// </summary>
        private async void Recommending()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            LastActive = DateTime.UtcNow;

            // First thing is to issue a buy if this asset it not in stock yet
            var stockAmount = await _accountant.GetBalance(BaseAsset).ConfigureAwait(true);

            if (stockAmount is null || stockAmount.Total == 0)
            {
                // Check if price is valid
                var latestAskPrice = _bestAskPrice.Latest;
                var latestBidPrice = _bestBidPrice.Latest;

                if (latestAskPrice is null || latestBidPrice is null)
                {
                    _logger.Verbose($"{Symbol.Name}: No price available");
                    return;
                }

                // Recommend buy
                _newRecommendation.OnNext(new Recommendation()
                {
                    Action = ActionType.Buy,
                    BestAskPrice = latestAskPrice.Value,
                    BestBidPrice = latestBidPrice.Value,
                });

                _logger.Information($"{Symbol.Name}: Recommended to buy");

                return;
            }

            // Use regular approach
            // Get current symbol price
            Point currentPrice = new Point(time: default, price: _bestBidPrice.GetAverage(), slope: 0);

            if (currentPrice.Value < 0)
            {
                _logger.Verbose($"Skipped {Symbol.Name} due to old or incomplete data: {currentPrice.Value:0.00}");

                return;
            }

            // Trade summary
            var tradeSummary = _accountant.GetTradeSummary(Symbol.Name);
            if (tradeSummary is null)
            {
                _logger.Verbose($"{Symbol.Name}: No Trade Summary available");
                return;
            }

            _logger.Debug($"{Symbol.Name}: Trade Summary {tradeSummary.Quantity} @ {tradeSummary.PricePerUnit} (eff. {tradeSummary.QuoteQuantity,2}");

            // Make the decision
            var action = ActionType.Hold;

            if (_thresholds.IsDroppingBelowThreshold(currentPrice.Value))
            {
                action = ActionType.Sell;
            }

            if (action != ActionType.Hold)
            {
                _logger.Debug($"{Symbol.Name}: {action} @ {currentPrice.Value}");

                // Instantiate new recommendation
                _newRecommendation.OnNext(new Recommendation()
                {
                    Action = action,
                    Price = currentPrice.Value,
                    BestAskPrice = _bestAskPrice.GetAverage(),
                    BestBidPrice = _bestBidPrice.GetAverage(),
                });
            }

            stopwatch.Stop();

            _logger.Debug($"{Symbol.Name}: Recommendation time: {stopwatch.Elapsed.TotalMilliseconds}");
        }

        #endregion

        #region Start / Terminate

        /// <summary>
        /// Starts the <c>Analyst</c> instance
        /// </summary>
        public async Task Start(BinanceSymbol symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(paramName: nameof(symbol));

            if (_jobRecommender.Enabled)
            {
                _logger.Warning($"{symbol}: Analyst is already active");
                return;
            }

            _logger.Information($"{BaseAsset}: Starting Analyst");

            Symbol = symbol;

            #region Subscriptions

            // Subscribe to symbol
            var result = await _binanceSocketClient.Spot.SubscribeToBookTickerUpdatesAsync(Symbol.Name,
                async (BinanceStreamBookPrice bsbp) =>
                {
                    _currentPrices.Enqueue(new CurrentBookPrice(bsbp));

                    if (_thresholds is null)
                    {
                        _thresholds = new Thresholds(bsbp.BestAskPrice);
                    }

                    // Record rise
                    Task hitPrice = _thresholds.Hit(bsbp.BestAskPrice);

                    // Record asking and bidding price
                    Task addBestAskPrice = _bestAskPrice.Add(bsbp.BestAskPrice);
                    Task addBestBidPrice = _bestBidPrice.Add(bsbp.BestBidPrice);

                    await Task.WhenAll(hitPrice, addBestAskPrice, addBestBidPrice).ConfigureAwait(true);

                    _logger.Verbose($"{bsbp.Symbol}: Book tick update - asking is {bsbp.BestAskPrice:0.00}");
                    _logger.Verbose($"{bsbp.Symbol}: Book tick update - bidding is {bsbp.BestBidPrice:0.00}");

                }).ConfigureAwait(true);

            #endregion

            if (!result.Success)
            {
                IsFaulty = true;
                _logger.Warning($"{BaseAsset}: Analyst is FAULTY");
            }
            else
            {
                _subscription = result.Data;

                // Do not start recommender when state is faulty
                _jobRecommender.Start();
                _logger.Information($"{BaseAsset}: Analyst started");
            }
        }

        /// <summary>
        /// Stops the <c>Analyst</c> instance
        /// </summary>
        public async Task Terminate()
        {
            // Stop recommendation maker
            _jobRecommender.Terminate();

            _newRecommendation.OnCompleted();

            // Terminate possible running tasks
            _cancellationTokenSource.Cancel();

            if (_subscription is not null)
            {
                await _binanceSocketClient.Unsubscribe(_subscription).ConfigureAwait(true);
            }

            _logger.Information("Analyst stopped");

            await Task.CompletedTask.ConfigureAwait(false);
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
                _cancellationTokenSource.Dispose();
                _jobRecommender.Dispose();
                _newRecommendation.Dispose();
                _bestAskPrice.Dispose();
                _bestBidPrice.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
