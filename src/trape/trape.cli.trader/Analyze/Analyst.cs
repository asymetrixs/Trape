using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Analyze.Models;
using trape.cli.trader.Cache;
using trape.datalayer;
using trape.datalayer.Models;
using trape.jobs;
using Action = trape.datalayer.Enums.Action;

namespace trape.cli.trader.Analyze
{
    /// <summary>
    /// This class represents an analyst. It's task is to make recommendations on
    /// buying, keeping (wait), or selling assets based on different facts (slope, moving average, etc.)
    /// </summary>
    public class Analyst : IAnalyst
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Buffer
        /// </summary>
        private readonly IBuffer _buffer;

        /// <summary>
        /// Cancellation Token
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Timer when Analyst makes a new decision
        /// </summary>
        private readonly Job _jobRecommendationMaker;

        /// <summary>
        /// The last recommendation for an symbol
        /// </summary>
        private readonly Dictionary<string, Recommendation> _lastRecommendation;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Synchronizes access to recommendation generation
        /// </summary>
        private SemaphoreSlim _recommendationSynchronizer;

        /// <summary>
        /// Saves state of last strategy
        /// </summary>
        private Dictionary<string, Analysis> _lastAnalysis;

        /// <summary>
        /// Used to limit trend logging
        /// </summary>
        private int _logTrendLimiter;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new instance of the <c>Analyst</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="buffer">Buffer</param>
        public Analyst(ILogger logger, IBuffer buffer)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _ = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            this._logger = logger.ForContext<Analyst>();
            this._buffer = buffer;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._lastRecommendation = new Dictionary<string, Recommendation>();
            this._disposed = false;
            this._recommendationSynchronizer = new SemaphoreSlim(1, 1);
            this._lastAnalysis = new Dictionary<string, Analysis>();
            this._logTrendLimiter = 61;

            // Set up timer that makes decisions, every second
            this._jobRecommendationMaker = new Job(new TimeSpan(0, 0, 0, 0, 250), _makeRecommendation, this._cancellationTokenSource.Token);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Take Profit threshold
        /// </summary>
        public const decimal TakeProfitLimit = 0.991M;

        #endregion

        #region Methods

        /// <summary>
        /// Create a new decision for every symbol
        /// </summary>
        private async void _makeRecommendation()
        {
            // Check if free to go
            if (this._recommendationSynchronizer.CurrentCount == 0)
            {
                return;
            }

            try
            {
                // Get lock
                this._recommendationSynchronizer.Wait();

                foreach (var symbol in this._buffer.GetSymbols())
                {
                    this._logger.Verbose($"{symbol}: Calculating recommendation");

                    await _recommend(symbol).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                this._logger.Error(ex.Message, ex);
                this._logger.Error(ex.StackTrace);
            }
            finally
            {
                // Release lock
                this._recommendationSynchronizer.Release();
            }
        }

        /// <summary>
        /// Make the actual decision for the symbol
        /// </summary>
        /// <param name="symbol">Symbol to evaluate</param>
        /// <returns></returns>
        private async Task _recommend(string symbol)
        {
            #region Argument checks

            if (string.IsNullOrEmpty(symbol))
            {
                this._logger.Warning("Symbol is empty, cancelling.");
                return;
            }

            #endregion

            // Placeholder for strategy
            if (!this._lastAnalysis.ContainsKey(symbol))
            {
                this._lastAnalysis.Add(symbol, new Analysis(symbol));
            }

            var analysis = this._lastAnalysis[symbol];
            analysis.PrepareForUpdate();

            // Get stats
            var stat3s = this._buffer.Stats3sFor(symbol);
            var stat15s = this._buffer.Stats15sFor(symbol);
            var stat2m = this._buffer.Stats2mFor(symbol);
            var stat10m = this._buffer.Stats10mFor(symbol);
            var stat2h = this._buffer.Stats2hFor(symbol);

            // Check that data is valud
            if (stat3s == null || !stat3s.IsValid()
                || stat15s == null || !stat15s.IsValid()
                || stat2m == null || !stat2m.IsValid()
                || stat10m == null || !stat10m.IsValid())
            {
                this._logger.Warning($"Skipped {symbol} due to old or incomplete data: 3s:{(stat3s == null ? false : stat3s?.IsValid())} " +
                    $"15s:{(stat15s == null ? false : stat15s?.IsValid())} " +
                    $"2m:{(stat2m == null ? false : stat2m?.IsValid())} " +
                    $"10m:{(stat10m == null ? false : stat10m?.IsValid())}");
                this._lastRecommendation.Remove(symbol);
                return;
            }

            // Use regular approach
            // Get current symbol price
            var currentPrice = new Point(time: default, price: this._buffer.GetBidPrice(symbol), slope: 0);
            var movingAverage1h = new Point(price: stat10m.MovingAverage1h, slope: stat10m.Slope1h, slopeBase: TimeSpan.FromHours(1));
            var movingAverage3h = new Point(price: stat10m.MovingAverage3h, slope: stat10m.Slope3h, slopeBase: TimeSpan.FromHours(3));
            var panicLimit = movingAverage3h * 0.9975M;
            var movav1hInterceptingPrice = currentPrice.WillInterceptWith(movingAverage1h);
            var priceInterceptingMovAv1h = movingAverage1h.WillInterceptWith(currentPrice.Price, slope: stat2m.Slope10m, slopeBase: 10 * 60);

            if (movav1hInterceptingPrice != null)
            {
                this._logger.Verbose($"movav1hInterceptingPrice: {movav1hInterceptingPrice.Price:0.00} {movav1hInterceptingPrice.Time.TotalMinutes:#0.00}m");
            }
            if (priceInterceptingMovAv1h != null)
            {
                this._logger.Verbose($"priceInterceptingMovAv1h: {priceInterceptingMovAv1h.Price:0.00} {priceInterceptingMovAv1h.Time.TotalMinutes:#0.00}m");
            }

            // Make the decision
            var action = Action.Hold;
            var lastFallingPrice = this._buffer.GetLastFallingPrice(symbol);
            if (lastFallingPrice != null)
            {
                this._logger.Verbose($"{symbol}: Last Falling Price Original: {lastFallingPrice.OriginalPrice:0.00} | Since: {lastFallingPrice.Since.ToShortTimeString()}");
            }

            var path = new StringBuilder();

            // Panic
            if (stat3s.Slope5s < -8
                && stat3s.Slope10s < -5
                && stat3s.Slope15s < -6
                && stat3s.Slope30s < -15
                && stat15s.Slope45s < -12
                && stat15s.Slope1m < -10
                && stat15s.Slope2m < -5
                && stat15s.Slope3m < -1
                // Define threshhold from when on panic mode is active
                && panicLimit < movingAverage3h
                // Price has to drop for more than 10 seconds
                && lastFallingPrice != null
                    && lastFallingPrice.Since < DateTime.UtcNow.AddSeconds(-10)
                )
            {
                // Panic sell
                action = Action.PanicSell;

                analysis.PanicDetected();

                this._logger.Warning($"{symbol}: {currentPrice.Price:0.00} - Panic Mode");
                path.Append("|panic");
            }
            // Jump increase
            else if (stat3s.Slope5s > 15
                    && stat3s.Slope10s > 10
                    && stat3s.Slope15s > 15
                    && stat3s.Slope30s > 9
                    && stat15s.Slope1m > 0)
            {
                path.Append("jump");
                // If Slope1h is negative, then only join the jumping trend if the current price
                // is higher than the value of the Slope1h in 15 minutes
                if (stat10m.Slope1h < -10)
                {
                    path.Append("a");
                    // Only jump if price goes higher than intercept with Slope1h in 15 minutes
                    var intercept15min = movingAverage1h.WillInterceptWith(currentPrice);
                    // 15 minutes
                    if (intercept15min?.Time < TimeSpan.FromMinutes(15))
                    {
                        this._logger.Verbose("[jump]");
                        action = Action.JumpBuy;
                        analysis.JumpDetected();
                        path.Append("|b");
                    }
                }
                // Slope1h is (almost) positive, always jump
                else
                {
                    path.Append("|b");
                    this._logger.Verbose("[jump]");
                    action = Action.JumpBuy;
                    analysis.JumpDetected();
                }
            }
            else if (stat2m.Slope10m < 0 && (currentPrice > movingAverage1h))
            {
                if (stat10m.Slope3h > 0)
                {
                    action = Action.Sell;
                    path.Append("|sell");
                }
                else
                {
                    action = Action.StrongSell;
                    path.Append("|strongsell");
                }
            }
            else if (stat2m.Slope7m > 0 && (currentPrice < movingAverage1h))
            {
                if (stat10m.Slope3h > 0)
                {
                    action = Action.StrongBuy;
                    path.Append("|strongbuy");
                }
                else
                {
                    action = Action.Buy;
                    path.Append("|buy");
                }
            }

            var calcAction = action;

            // If a race is ongoing or after it has stopped wait for 9 minutes for market to cool down
            // before another buy is made
            if (analysis.LastRaceEnded.AddMinutes(9) > DateTime.UtcNow
                && (action == Action.Buy || action == Action.JumpBuy || action == Action.StrongBuy))
            {
                this._logger.Verbose($"{symbol}: Race ended less than 9 minutes ago, don't buy.");
                action = Action.Hold;
                path.Append("_|race");
            }


            // If Panic mode ended, wait for 5 minutes before start buying again, except if jump
            if (analysis.LastPanicModeEnded.AddMinutes(5) > DateTime.UtcNow)
            {
                path.Append("_panicend");
                if (action == Action.Buy || action == Action.StrongBuy)
                {
                    this._logger.Verbose($"{symbol}: Panic mode ended less than 5 minutes ago, don't buy.");
                    action = Action.Hold;
                    path.Append("|a");
                }
            }
            // If Panic mode ended and action is not buy but trend is strongly upwards
            else if (analysis.LastPanicModeEnded.AddMinutes(7) > DateTime.UtcNow
                && action != Action.Buy
                && stat3s.Slope5s > 10 && stat3s.Slope10s > 10 && stat3s.Slope15s > 7.5M && stat3s.Slope30s > 0 && stat3s.Slope30s > 0 && stat10m.Slope3h > -64.8M
                && currentPrice < movingAverage1h)
            {
                this._logger.Verbose($"{symbol}: Panic mode ended more than 7 minutes ago and trend is strongly upwards, buy.");
                action = Action.Buy;
                path.Append("_|panicend");
            }


            // If strong sell happened or slope is too negative, do not buy immediately
            if ((analysis.GetLastDateOf(Action.StrongSell).AddMinutes(2) > DateTime.UtcNow || stat10m.Slope30m < -20M)
                && (action == Action.Buy || action == Action.JumpBuy || action == Action.StrongBuy))
            {
                this._logger.Verbose($"{symbol}: Last strong sell was less than 1 minutes ago, don't buy.");
                action = Action.Hold;
                path.Append("_|wait");
            }

            Point raceStartingPrice;
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    // Check if price has gained a lot over the last 30 minutes
                    // Get Price from 30 minutes ago
                    raceStartingPrice = new Point(time: TimeSpan.FromMinutes(-1),
                                                    price: await database.GetLowestPrice(symbol, DateTime.UtcNow.AddMinutes(-30), this._cancellationTokenSource.Token).ConfigureAwait(false),
                                                    slope: 0);
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                    raceStartingPrice = new Point();
                }
            }

            /// Advise to sell on <see cref="TakeProfitLimit"/> % gain
            if (raceStartingPrice.Price != default && raceStartingPrice < (currentPrice * TakeProfitLimit))
            {
                this._logger.Verbose($"{symbol}: Race detected at {currentPrice.Price:0.00}.");
                analysis.RaceDetected();
                path.Append("_racestart");

                // Check market movement, if a huge sell is detected advice to take profits
                if (stat3s.Slope10s < -10)
                {
                    action = Action.TakeProfitsSell;
                    this._logger.Verbose($"{symbol}: Race ended at {currentPrice.Price:0.00}.");
                    path.Append("|raceend");
                    analysis.RaceEnded();
                }
            }

            // Buy after PanicSell
            if (analysis.PanicSellHasEnded
                && (action != Action.StrongSell && action != Action.PanicSell)
                && currentPrice < movingAverage1h
                && analysis.BuyAfterPanicSell())
            {
                action = Action.Buy;
                path.Append("_|panicbuy");
            }


            // Print strategy changes
            if (analysis.Action != action)
            {
                this._logger.Information($"{symbol}: {currentPrice.Price:0.00} - Switching stategy: {analysis.Action} -> {action}");
                analysis.UpdateAction(action);
            }
            // Print strategy every hour in log
            else if (analysis.Now.Minute == 0 && analysis.Now.Second == 0 && analysis.Now.Millisecond < 100)
            {
                this._logger.Information($"{symbol}: Stategy: {action}");
            }

            this._logger.Debug($"{symbol}: {currentPrice.Price:0.00} Decision - Calculated / Final: {calcAction} / {action} - {path}");

            // Instantiate new recommendation
            var newRecommendation = new Recommendation()
            {
                Action = action,
                Price = currentPrice.Price,
                Symbol = symbol,
                CreatedOn = DateTime.UtcNow,
                Slope5s = stat3s.Slope5s,
                Slope10s = stat3s.Slope10s,
                Slope15s = stat3s.Slope15s,
                Slope30s = stat3s.Slope30s,
                Slope45s = stat15s.Slope45s,
                Slope1m = stat15s.Slope1m,
                Slope2m = stat15s.Slope2m,
                Slope3m = stat15s.Slope3m,
                Slope5m = stat2m.Slope5m,
                Slope7m = stat2m.Slope7m,
                Slope10m = stat2m.Slope10m,
                Slope15m = stat2m.Slope15m,
                Slope30m = stat10m.Slope30m,
                Slope1h = stat10m.Slope1h,
                Slope2h = stat10m.Slope2h,
                Slope3h = stat10m.Slope3h,
                Slope6h = stat2h.Slope6h,
                Slope12h = stat2h.Slope12h,
                Slope18h = stat2h.Slope18h,
                Slope1d = stat2h.Slope1d,
                MovingAverage5s = stat3s.MovingAverage5s,
                MovingAverage10s = stat3s.MovingAverage10s,
                MovingAverage15s = stat3s.MovingAverage15s,
                MovingAverage30s = stat3s.MovingAverage30s,
                MovingAverage45s = stat15s.MovingAverage45s,
                MovingAverage1m = stat15s.MovingAverage1m,
                MovingAverage2m = stat15s.MovingAverage2m,
                MovingAverage3m = stat15s.MovingAverage3m,
                MovingAverage5m = stat2m.MovingAverage5m,
                MovingAverage7m = stat2m.MovingAverage7m,
                MovingAverage10m = stat2m.MovingAverage10m,
                MovingAverage15m = stat2m.MovingAverage15m,
                MovingAverage30m = stat10m.MovingAverage30m,
                MovingAverage1h = stat10m.MovingAverage1h,
                MovingAverage2h = stat10m.MovingAverage2h,
                MovingAverage3h = stat10m.MovingAverage3h,
                MovingAverage6h = stat2h.MovingAverage6h,
                MovingAverage12h = stat2h.MovingAverage12h,
                MovingAverage18h = stat2h.MovingAverage18h,
                MovingAverage1d = stat2h.MovingAverage1d
            };

            // Store recommendation temporarily for <c>Analyst</c>
            Recommendation lastRecommendation = null;
            if (this._lastRecommendation.ContainsKey(symbol))
            {
                lastRecommendation = this._lastRecommendation.GetValueOrDefault(symbol);
                this._lastRecommendation[symbol] = newRecommendation;
            }
            else
            {
                this._lastRecommendation.Add(symbol, newRecommendation);
            }

            this._logger.Verbose($"{symbol}: Recommending: {action}");

            // Store recommendation in database
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    database.Recommendations.Add(newRecommendation);
                    await database.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                }
            }

            _logTrend(newRecommendation, stat10m, stat2h);
        }

        /// <summary>
        /// Returns a string representing current trend data
        /// </summary>
        /// <param name="recommendation">The recommendation that was calculated</param>
        /// <param name="stat10m">10 minutes stats</param>
        /// <param name="stat2Hours">2 hours stats</param>
        /// <returns>String that returns current trends</returns>
        private void _logTrend(Recommendation recommendation, Stats10m stat10m, Stats2h stat2Hours)
        {
            // Announce the trend every second for reduced log spamming
            if (DateTime.UtcNow.Second != this._logTrendLimiter)
            {
                this._logTrendLimiter = DateTime.UtcNow.Second;

                var reco = recommendation.Action == datalayer.Enums.Action.Buy ? "Buy :" : recommendation.Action.ToString();

                this._logger.Verbose($"{recommendation.Symbol}: {reco} | S1h: {stat10m.Slope1h:0.0000} | S2h: {stat10m.Slope2h:0.0000} | MA1h: {stat10m.MovingAverage1h:0.0000} | MA2h: {stat10m.MovingAverage2h:0.0000} | MA6h: {stat2Hours.MovingAverage6h:0.0000}");
            }
        }

        /// <summary>
        /// Returns the latest recommendation for the given symbol
        /// </summary>
        /// <param name="symbol">The symbol for which the recommendation is requested</param>
        /// <returns>The recommendation for the symbol</returns>
        public Recommendation GetRecommendation(string symbol)
        {
            // Decision must be present and not older than 5 seconds
            if (this._lastRecommendation.TryGetValue(symbol, out Recommendation decision)
                && decision.CreatedOn > DateTime.UtcNow.AddSeconds(-5))
            {
                return decision;
            }

            return null;
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Starts the <c>Analyst</c> instance
        /// </summary>
        public void Start()
        {
            // Start recommendation maker
            this._jobRecommendationMaker.Start();

            this._logger.Information("Analyst started");
        }

        /// <summary>
        /// Stops the <c>Analyst</c> instance
        /// </summary>
        public void Terminate()
        {
            // Stop recommendation maker
            this._jobRecommendationMaker.Terminate();

            // Terminate possible running tasks
            this._cancellationTokenSource.Cancel();

            this._logger.Information("Analyst stopped");
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
                this._cancellationTokenSource.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
