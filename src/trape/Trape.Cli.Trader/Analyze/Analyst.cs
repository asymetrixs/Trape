using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.MarketStream;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.trader.Account;
using Trape.Cli.trader.Analyze.Models;
using Trape.Cli.trader.Listener;
using Trape.Cli.trader.Team;
using Trape.Cli.Trader.Analyze.Models;
using Trape.Jobs;

namespace Trape.Cli.trader.Analyze
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
        private List<UpdateSubscription> _subscriptions;

        /// <summary>
        /// Timer when Analyst makes a new decision
        /// </summary>
        private readonly Job _jobRecommender;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Saves state of last strategy
        /// </summary>
        private Analysis _lastAnalysis;

        /// <summary>
        /// Used to limit trend logging
        /// </summary>
        private int _logTrendLimiter;

        /// <summary>
        /// Binance Stream Tick Buffer
        /// </summary>
        private readonly ConcurrentBag<IBinanceTick> _binanceStreamTickBuffer;

        /// <summary>
        /// Binance Stream Kline Data Buffer
        /// </summary>
        private readonly KLineDiagrams _binanceStreamKlineDataBuffer;

        /// <summary>
        /// Recommendations
        /// </summary>
        private readonly Subject<Recommendation> _newRecommendation;

        /// <summary>
        /// Current prices
        /// </summary>
        private readonly ConcurrentQueue<CurrentBookPrice> _currentPrices;

        /// <summary>
        /// Holds the last time per symbol when the price dropped for the first time
        /// </summary>
        private readonly FallingPrice? _fallingPrices;

        /// <summary>
        /// Best bid price
        /// </summary>
        private readonly BestPrice _bestBidPrice;

        /// <summary>
        /// Best ask price
        /// </summary>
        private readonly BestPrice _bestAskPrice;

        private static readonly TimeSpan _5s = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan _10s = TimeSpan.FromSeconds(10);

        private static readonly TimeSpan _15s = TimeSpan.FromSeconds(15);

        private static readonly TimeSpan _30s = TimeSpan.FromSeconds(30);

        private static readonly TimeSpan _45s = TimeSpan.FromSeconds(45);

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
            _disposed = false;
            _logTrendLimiter = 61;
            _binanceStreamTickBuffer = new ConcurrentBag<IBinanceTick>();
            _binanceStreamKlineDataBuffer = new KLineDiagrams();
            _subscriptions = new List<UpdateSubscription>();
            _newRecommendation = new Subject<Recommendation>();
            _currentPrices = new ConcurrentQueue<CurrentBookPrice>();
            IsFaulty = false;
            _bestBidPrice = new BestPrice();
            _bestAskPrice = new BestPrice();

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
            LastActive = DateTime.UtcNow;

            // First thing is to issue a buy if this asset it not in stock yet
            var stockAmount = await _accountant.GetBalance(BaseAsset).ConfigureAwait(true);

            if (stockAmount is null || stockAmount.Total == 0)
            {
                var latestAskPrice = _bestAskPrice.Latest;
                var latestBidPrice = _bestBidPrice.Latest;

                if (latestAskPrice is null || latestBidPrice is null)
                {
                    _logger.Verbose($"{Symbol.Name}: No price available");
                    return;
                }

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
            var currentPrice = new Point(time: default, price: _bestBidPrice.GetAverage(), slope: 0);

            if (currentPrice.Value < 0)
            {
                _logger.Verbose($"Skipped {Symbol.Name} due to old or incomplete data: {currentPrice.Value:0.00}");

                return;
            }

            // Make the decision
            var action = ActionType.Hold;
            var lastFallingPrice = _fallingPrices;
            if (lastFallingPrice != null)
            {
                _logger.Verbose($"{Symbol.Name}: Last Falling Price Original: {lastFallingPrice.OriginalPrice:0.00} | Since: {lastFallingPrice.Since.ToShortTimeString()}");
            }

            var tradeSummary = _accountant.GetTradeSummary(Symbol.Name);

            if (tradeSummary is null)
            {
                _logger.Verbose($"{Symbol.Name}: No Trade Summary available");
                return;
            }

            _logger.Debug($"{Symbol.Name}: Trade Summary {tradeSummary.Quantity} @ {tradeSummary.PricePerUnit} (eff. {tradeSummary.QuoteQuantity,2}");

            _lastAnalysis.PrepareForUpdate();

            var path = new StringBuilder();

            var slope5s = Slope(_5s);
            var slope10s = Slope(_10s);
            var slope15s = Slope(_15s);
            var slope30s = Slope(_30s);
            var slope45s = Slope(_45s);


            // Panic, threshold is relative to price 
            if (slope5s < -currentPrice.Value.XPartOf(8)
                && slope10s < -currentPrice.Value.XPartOf(5)
                && slope15s < -currentPrice.Value.XPartOf(6)
                && slope30s < -currentPrice.Value.XPartOf(15))
            {
                // Panic sell
                action = ActionType.Sell;

                _lastAnalysis.PanicDetected();

                _logger.Warning($"{Symbol.Name}: {currentPrice.Value:0.00} - Panic Mode");
                path.Append("|panic");
            }
            // Jump increase
            else if (slope5s > currentPrice.Value.XPartOf(15)
                    && slope10s > currentPrice.Value.XPartOf(1)
                    && slope15s > currentPrice.Value.XPartOf(15)
                    && slope30s > currentPrice.Value.XPartOf(9))
            {
                path.Append("jump");
                _logger.Verbose("[jump]");
                action = ActionType.Buy;
                _lastAnalysis.JumpDetected();
            }

            // Cache action
            var calcAction = action;

            // If a race is ongoing or after it has stopped wait for 5 minutes for market to cool down
            // before another buy is made
            if (_lastAnalysis.LastRaceEnded.AddMinutes(5) > DateTime.UtcNow && action == ActionType.Buy)
            {
                _logger.Verbose($"{BaseAsset}: Race ended less than 9 minutes ago, don't buy.");
                action = ActionType.Hold;
                path.Append("_|race");
            }

            // If Panic mode ended, wait for 5 minutes before start buying again, except if jump
            if (_lastAnalysis.LastPanicModeEnded.AddMinutes(5) > DateTime.UtcNow)
            {
                path.Append("_panicend");
            }

            Point raceStartingPrice;
            try
            {
                var s20Ago = DateTime.Now.AddSeconds(-25);
                raceStartingPrice = new Point(time: TimeSpan.FromMinutes(-1),
                                                price: _currentPrices.Where(c => c.On > s20Ago).Min(c => c.BestAskPrice),
                                                slope: 0);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                raceStartingPrice = new Point();
            }

            var raceIdentifier = 0.7M;
            if (raceStartingPrice.Value != default && raceStartingPrice < (currentPrice * raceIdentifier))
            {
                _logger.Verbose($"{BaseAsset}: Race detected at {currentPrice.Value:0.00}.");
                _lastAnalysis.RaceDetected();
                path.Append("_racestart");

                // Check market movement, if a huge sell is detected advice to take profits
                if (slope10s < -currentPrice.Value.XPartOf(10))
                {
                    action = ActionType.Sell;
                    _logger.Verbose($"{BaseAsset}: Race ended at {currentPrice.Value:0.00}.");
                    path.Append("|raceend");
                    _lastAnalysis.RaceEnded();
                }
            }

            // Print strategy changes
            if (_lastAnalysis.Action != action)
            {
                _logger.Information($"{BaseAsset}: {currentPrice.Value:0.00} - Switching stategy: {_lastAnalysis.Action} -> {action}");
                _lastAnalysis.UpdateAction(action);
            }

            _logger.Debug($"{BaseAsset}: {currentPrice.Value:0.00} Decision - Calculated / Final: {calcAction} / {action} - {path}");

            //// Instantiate new recommendation
            var newRecommendation = new Recommendation()
            {
                Action = action,
                Price = currentPrice.Value,
                BestAskPrice = _bestAskPrice.GetAverage(),
                BestBidPrice = _bestBidPrice.GetAverage(),
                CreatedOn = DateTime.UtcNow
            };

            //var oldRecommendation = _listener.GetRecommendation(BaseAsset);
            //_listener.UpdateRecommendation(newRecommendation);

            //_logger.Verbose($"{BaseAsset}: Recommending: {action}");

            //if (oldRecommendation.Action != newRecommendation.Action)
            //{
            //    _logger.Information($"{BaseAsset}: Recommendation changed: {oldRecommendation.Action} -> {newRecommendation.Action}");
            //}

            //// Store recommendation in database
            //using (AsyncScopedLifestyle.BeginScope(Program.Container))
            //{
            //    var database = Program.Container.GetService<TrapeContext>();
            //    try
            //    {
            //        database.Recommendations.Add(newRecommendation);
            //        await database.SaveChangesAsync().ConfigureAwait(false);
            //    }
            //    catch (Exception e)
            //    {
            //        _logger.Error(e, e.Message);
            //    }
            //}

            if (action != ActionType.Hold)
            {
                _newRecommendation.OnNext(newRecommendation);
            }
        }

        /// <summary>
        /// Returns the change in percent in a given timespan compared to now.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="timespan">Interval</param>
        /// <returns></returns>
        public decimal? Slope(TimeSpan timespan)
        {
            var ordered = _currentPrices.Where(d => d.On >= DateTime.Now.Add(timespan)).OrderByDescending(s => s.On);
            var latest = ordered.FirstOrDefault();
            var oldest = ordered.LastOrDefault();

            if (latest is null || oldest is null)
            {
                return null;
            }

            // normalize
            var divider = latest.On.Ticks - oldest.On.Ticks;

            if (divider == 0)
            {
                return null;
            }

            return (latest.BestAskPrice - oldest.BestAskPrice) / divider;
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

            var subscribeTasks = new List<Task<CallResult<UpdateSubscription>>>();

            #region Subscriptions

            // Subscribe to all symbols
            var updateSubscription = await _binanceSocketClient.Spot.SubscribeToBookTickerUpdatesAsync(Symbol.Name, async (BinanceStreamBookPrice bsbp) =>
                {
                    _currentPrices.Enqueue(new CurrentBookPrice(bsbp));

                    Task addBestAskPrice = _bestAskPrice.Add(bsbp.BestAskPrice);
                    Task addBestBidPrice = _bestBidPrice.Add(bsbp.BestBidPrice);

                    await Task.WhenAll(addBestAskPrice, addBestBidPrice).ConfigureAwait(true);

                    _logger.Verbose($"{bsbp.Symbol}: Book tick update - asking is {bsbp.BestAskPrice:0.00}");
                    _logger.Verbose($"{bsbp.Symbol}: Book tick update - bidding is {bsbp.BestBidPrice:0.00}");

                }).ConfigureAwait(true);

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToSymbolTickerUpdatesAsync(Name, (IBinanceTick bt) => _binanceStreamTickBuffer.Add(bt)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.OneMinute, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.ThreeMinutes, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.FiveMinutes, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.FifteenMinutes, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.ThirtyMinutes, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.OneHour, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.TwoHour, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.SixHour, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.EightHour, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.TwelveHour, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.OneDay, (IBinanceStreamKlineData bskd) => _binanceStreamKlineDataBuffer.Update(bskd)));

            #endregion

            await Task.WhenAll(subscribeTasks.ToArray()).ConfigureAwait(true);

            foreach (var t in subscribeTasks)
            {
                var result = t.Result;

                if (result.Success)
                {
                    _subscriptions.Add(result.Data);
                }
                else
                {
                    IsFaulty = true;
                }
            }

            _jobRecommender.Start();

            _logger.Information($"{BaseAsset}: Analyst started");
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
