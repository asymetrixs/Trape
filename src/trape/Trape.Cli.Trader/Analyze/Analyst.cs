using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketData;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
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
using Trape.Datalayer;
using Trape.Datalayer.Models;
using Trape.Jobs;
using Action = Trape.Datalayer.Enums.Action;

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
        /// Listener
        /// </summary>
        private readonly IListener _listener;

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
        private readonly ConcurrentBag<IBinanceStreamKlineData> _binanceStreamKlineDataBuffer;

        /// <summary>
        /// Recommendations
        /// </summary>
        private readonly Subject<Recommendation> _newRecommendation;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new instance of the <c>Analyst</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="buffer">Buffer</param>
        /// /// <param name="accountant">Accountant</param>
        public Analyst(ILogger logger, IListener buffer, IAccountant accountant, IBinanceSocketClient binanceSocketClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _listener = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            #endregion

            _logger = logger.ForContext<Analyst>();
            _binanceSocketClient = binanceSocketClient;
            _cancellationTokenSource = new CancellationTokenSource();
            _disposed = false;
            _logTrendLimiter = 61;
            _binanceStreamTickBuffer = new ConcurrentBag<IBinanceTick>();
            _binanceStreamKlineDataBuffer = new ConcurrentBag<IBinanceStreamKlineData>();
            _subscriptions = new List<UpdateSubscription>();
            _newRecommendation = new Subject<Recommendation>();
            IsFaulty = false;

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
        /// Take Profit threshold
        /// </summary>
        public const decimal TakeProfitLimit = 0.991M;

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

            _lastAnalysis.PrepareForUpdate();

            // Use regular approach
            // Get current symbol price
            var currentPrice = new Point(time: default, price: _listener.GetBidPrice(BaseAsset), slope: 0);

            if (currentPrice.Value < 0)
            {
                _logger.Verbose($"Skipped {BaseAsset} due to old or incomplete data: {currentPrice.Value:0.00}");

                _listener.UpdateRecommendation(new Recommendation() { Symbol = BaseAsset, Action = Action.Hold });
                return;
            }

            // Make the decision
            var action = Action.Hold;
            var lastFallingPrice = _listener.GetLastFallingPrice(BaseAsset);
            if (lastFallingPrice != null)
            {
                _logger.Verbose($"{BaseAsset}: Last Falling Price Original: {lastFallingPrice.OriginalPrice:0.00} | Since: {lastFallingPrice.Since.ToShortTimeString()}");
            }

            var x = this._binanceStreamKlineDataBuffer.Where(b => b.Data.Interval == KlineInterval.OneMinute);

            var path = new StringBuilder();

            //// Panic, threshold is relative to price 
            //if (stat3s.Slope5s < -currentPrice.Value.XPartOf(8)
            //    && stat3s.Slope10s < -currentPrice.Value.XPartOf(5)
            //    && stat3s.Slope15s < -currentPrice.Value.XPartOf(6)
            //    && stat3s.Slope30s < -currentPrice.Value.XPartOf(15))
            //{
            //    // Panic sell
            //    action = Action.PanicSell;

            //    _lastAnalysis.PanicDetected();

            //    _logger.Warning($"{BaseAsset}: {currentPrice.Value:0.00} - Panic Mode");
            //    path.Append("|panic");
            //}
            //// Jump increase
            //else if (stat3s.Slope5s > currentPrice.Value.XPartOf(15)
            //        && stat3s.Slope10s > currentPrice.Value.XPartOf(1)
            //        && stat3s.Slope15s > currentPrice.Value.XPartOf(15)
            //        && stat3s.Slope30s > currentPrice.Value.XPartOf(9))
            //{
            //    path.Append("jump");
            //    _logger.Verbose("[jump]");
            //    action = Action.JumpBuy;
            //    _lastAnalysis.JumpDetected();
            //}

            //// Cache action
            //var calcAction = action;

            //// If a race is ongoing or after it has stopped wait for 5 minutes for market to cool down
            //// before another buy is made
            //if (_lastAnalysis.LastRaceEnded.AddMinutes(5) > DateTime.UtcNow
            //    && (action == Action.Buy || action == Action.JumpBuy || action == Action.StrongBuy))
            //{
            //    _logger.Verbose($"{BaseAsset}: Race ended less than 9 minutes ago, don't buy.");
            //    action = Action.Hold;
            //    path.Append("_|race");
            //}

            //// If Panic mode ended, wait for 5 minutes before start buying again, except if jump
            //if (_lastAnalysis.LastPanicModeEnded.AddMinutes(5) > DateTime.UtcNow)
            //{
            //    path.Append("_panicend");
            //    if (action == Action.Buy || action == Action.StrongBuy)
            //    {
            //        _logger.Verbose($"{BaseAsset}: Panic mode ended less than 5 minutes ago, don't buy.");
            //        action = Action.Hold;
            //        path.Append("|a");
            //    }
            //}

            //// If strong sell happened or slope is too negative, do not buy immediately
            //if ((_lastAnalysis.GetLastDateOf(Action.StrongSell).AddMinutes(2) > DateTime.UtcNow)
            //    && (action == Action.Buy || action == Action.JumpBuy || action == Action.StrongBuy))
            //{
            //    _logger.Verbose($"{BaseAsset}: Last strong sell was less than 1 minutes ago, don't buy.");
            //    action = Action.Hold;
            //    path.Append("_|wait");
            //}

            //Point raceStartingPrice;
            //try
            //{
            //    // Check if price has gained a lot over the last 30 minutes
            //    // Get Price from 30 minutes ago
            //    raceStartingPrice = new Point(time: TimeSpan.FromMinutes(-5),
            //                                    price: _listener.GetLowestPrice(BaseAsset, _300s),
            //                                    slope: 0);
            //}
            //catch (Exception e)
            //{
            //    _logger.Error(e, e.Message);
            //    raceStartingPrice = new Point();
            //}

            ///// Advise to sell on <see cref="TakeProfitLimit"/> % gain
            //var raceIdentifier = TakeProfitLimit;
            //if (currentPrice.Value < 100)
            //{
            //    raceIdentifier = 0.97M;
            //}

            //if (raceStartingPrice.Value != default && raceStartingPrice < (currentPrice * raceIdentifier))
            //{
            //    _logger.Verbose($"{BaseAsset}: Race detected at {currentPrice.Value:0.00}.");
            //    _lastAnalysis.RaceDetected();
            //    path.Append("_racestart");

            //    // Check market movement, if a huge sell is detected advice to take profits
            //    if (stat3s.Slope10s < -currentPrice.Value.XPartOf(10))
            //    {
            //        action = Action.TakeProfitsSell;
            //        _logger.Verbose($"{BaseAsset}: Race ended at {currentPrice.Value:0.00}.");
            //        path.Append("|raceend");
            //        _lastAnalysis.RaceEnded();
            //    }
            //}

            //// Print strategy changes
            //if (_lastAnalysis.Action != action)
            //{
            //    _logger.Information($"{BaseAsset}: {currentPrice.Value:0.00} - Switching stategy: {_lastAnalysis.Action} -> {action}");
            //    _lastAnalysis.UpdateAction(action);
            //}
            //// Print strategy every hour in log
            //else if (_lastAnalysis.Now.Minute == 0 && _lastAnalysis.Now.Second == 0 && _lastAnalysis.Now.Millisecond < 100)
            //{
            //    _logger.Information($"{BaseAsset}: Stategy: {action}");
            //}

            //_logger.Debug($"{BaseAsset}: {currentPrice.Value:0.00} Decision - Calculated / Final: {calcAction} / {action} - {path}");

            //// Instantiate new recommendation
            var newRecommendation = new Recommendation()
            {
                Action = action,
                Price = currentPrice.Value,
                Symbol = BaseAsset,
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

            _newRecommendation.OnNext(newRecommendation);
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

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToSymbolTickerUpdatesAsync(Name, (IBinanceTick bt) =>
            {
                _binanceStreamTickBuffer.Add(bt);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.OneMinute, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.ThreeMinutes, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.FiveMinutes, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.FifteenMinutes, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.ThirtyMinutes, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.OneHour, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.TwoHour, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.SixHour, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.EightHour, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.TwelveHour, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

            subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(Name, KlineInterval.OneDay, (IBinanceStreamKlineData bskd) =>
            {
                _binanceStreamKlineDataBuffer.Add(bskd);
            }));

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
            }

            _disposed = true;
        }

        #endregion
    }
}
