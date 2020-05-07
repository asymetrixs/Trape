using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Cache;
using Microsoft.Extensions.DependencyInjection;
using trape.datalayer;
using trape.datalayer.Models;
using trape.jobs;

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
        /// Accountant
        /// </summary>
        private readonly IAccountant _accountant;

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
        private Dictionary<string, datalayer.Enums.Action> _lastStrategy;

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
        public Analyst(ILogger logger, IBuffer buffer, IAccountant accountant)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _ = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _ = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            #endregion

            this._logger = logger.ForContext<Analyst>();
            this._buffer = buffer;
            this._accountant = accountant;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._lastRecommendation = new Dictionary<string, Recommendation>();
            this._disposed = false;
            this._recommendationSynchronizer = new SemaphoreSlim(1, 1);
            this._lastStrategy = new Dictionary<string, datalayer.Enums.Action>();
            this._logTrendLimiter = 61;

            // Set up timer that makes decisions, every second
            this._jobRecommendationMaker = new Job(new TimeSpan(0, 0, 0, 0, 100), _makeRecommendation, this._cancellationTokenSource.Token);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create a new decision for every symbol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _makeRecommendation()
        {
            // Check if free to go
            if (this._recommendationSynchronizer.CurrentCount == 0)
            {
                return;
            }

            this._logger.Verbose("Calculating recommendation");

            try
            {
                // Get lock
                this._recommendationSynchronizer.Wait();

                foreach (var symbol in this._buffer.GetSymbols())
                {
                    await _recommend(symbol).ConfigureAwait(true);
                }
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
                return;
            }

            #endregion

            // Placeholder for strategy
            if (!this._lastStrategy.ContainsKey(symbol))
            {
                this._lastStrategy.Add(symbol, datalayer.Enums.Action.Hold);
            }

            var database = new TrapeContext(Program.Services.GetService<DbContextOptions<TrapeContext>>(), Program.Services.GetService<ILogger>());

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
                || stat10m == null || !stat10m.IsValid()
                /*|| stat2h == null || !stat2h.IsValid()*/)
            {
                this._logger.Warning($"Skipped {symbol} due to old or incomplete data: 3s:{stat3s?.IsValid()} 15s:{stat15s?.IsValid()} 2m:{stat2m?.IsValid()} 10m:{stat10m?.IsValid()} 2h:{stat2h?.IsValid()}");
                this._lastRecommendation.Remove(symbol);
                return;
            }

            // Use regular approach
            // Get current symbol price
            var currentPrice = this._buffer.GetBidPrice(symbol);

            // Corridor around Moving Average 10m
            decimal upperLimitMA1h = stat10m.MovingAverage1h * 1.0002M;
            decimal lowerLimitMA1h = stat10m.MovingAverage1h * 0.9998M;
            decimal panicLimitMA1h = stat10m.MovingAverage1h * 0.9975M;

            // MA3h * 1.008 (0.8%) is higher than MA1h AND Slope1h and Slope3h are positive
            var distanceOkAndTrendPositive = (stat10m.MovingAverage3h * 1.008M) > stat10m.MovingAverage1h
                                                && stat10m.Slope1h > -0.0065M && stat10m.Slope3h > 0.009M;

            const decimal strongThreshold = 0.004M;

            // Make the decision
            var action = datalayer.Enums.Action.Hold;

            var lastFallingPrice = this._buffer.GetLastFallingPrice(symbol);

            if (lastFallingPrice != null)
            {
                this._logger.Verbose($"{symbol}: Last Falling Price Original: {lastFallingPrice.OriginalPrice:0.00} | Since: {lastFallingPrice.Since.ToShortTimeString()}");
            }
            this._logger.Verbose($"{symbol}: {currentPrice:0.00} - ulMA1h: {upperLimitMA1h:0.0000} | llMA1h: {lowerLimitMA1h:0.0000} | plMA1h: {panicLimitMA1h:0.0000} | distance&Trend: {distanceOkAndTrendPositive}");

            // Panic
            if (stat3s.Slope5s < -2M
                && stat3s.Slope10s < -1.1M
                && stat3s.Slope15s < -0.5M
                && stat3s.Slope30s < -0.32M
                && stat15s.Slope45s < -0.11M
                && stat15s.Slope1m < -0.09M
                && stat15s.Slope2m < -0.05M
                && stat15s.Slope3m < -0.008M
                && stat2m.Slope5m < -0M
                // Define threshhold from when on panic mode is active
                && panicLimitMA1h < stat10m.MovingAverage3h
                // Price has to drop for more than 21 seconds
                // and lose more than 0.55%
                && lastFallingPrice != null
                    && lastFallingPrice.Since < DateTime.UtcNow.AddSeconds(-21)
                    && currentPrice < lastFallingPrice.OriginalPrice * 0.9955M
                )
            {
                // Panic sell
                action = datalayer.Enums.Action.PanicSell;
                this._logger.Warning("Panic Mode");
            }
            else if (stat10m.Slope30m > strongThreshold || distanceOkAndTrendPositive)
            {
                action = StrongBuyStrategy(stat3s, stat15s, stat2m, stat10m, stat2h, lowerLimitMA1h, upperLimitMA1h, distanceOkAndTrendPositive);
            }
            else if (stat10m.Slope30m > 0.0015M || distanceOkAndTrendPositive)
            {
                action = NormalBuyStrategy(stat3s, stat15s, stat2m, stat10m, stat2h, lowerLimitMA1h, upperLimitMA1h, distanceOkAndTrendPositive);
            }
            else if (stat10m.Slope30m < -strongThreshold && !distanceOkAndTrendPositive)
            {
                action = StrongSellStrategy(stat3s, stat15s, stat2m, stat10m, stat2h, lowerLimitMA1h, upperLimitMA1h);
            }
            else if (stat10m.Slope30m < -0.0015M && !distanceOkAndTrendPositive)
            {
                action = NormalSellStrategy(stat3s, stat15s, stat2m, stat10m, stat2h, lowerLimitMA1h, upperLimitMA1h);
            }

            // Check if price has gained a lot over the last 15 minutes
            // Get Price from 15 minutes ago
            var raceStartingPrice = await database.GetPriceOn(symbol, DateTime.UtcNow.AddMinutes(-15), this._cancellationTokenSource.Token).ConfigureAwait(false);

            // Advise to sell on 200 USD gain
            if (raceStartingPrice != default && raceStartingPrice < currentPrice - 220)
            {
                // Check market movement, if a huge sell is detected advice to take profits
                if (stat3s.MovingAverage15s < -2)
                {
                    action = datalayer.Enums.Action.TakeProfitsSell;
                }
            }

            var now = DateTime.UtcNow;
            // Print strategy changes
            if (this._lastStrategy[symbol] != action)
            {
                this._logger.Information($"{symbol}: {currentPrice:0.00} - Switching stategy: {this._lastStrategy[symbol]} -> {action}");
                this._lastStrategy[symbol] = action;
            }
            // Print strategy every hour in log
            else if (now.Minute == 0 && now.Second == 0 && now.Millisecond < 100)
            {
                this._logger.Information($"{symbol}: Stategy: {action}");
            }

            // Instantiate new recommendation
            var newRecommendation = new Recommendation()
            {
                Action = action,
                Price = currentPrice,
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

            this._logger.Verbose($"{symbol}: Recommending: {action}");

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

            // Store recommendation in database
            try
            {
                database.Recommendations.Add(newRecommendation);
                await database.SaveChangesAsync();
            }
            catch (Exception e)
            {
                this._logger.Error(e.Message, e);
            }
            finally
            {
                
            }

            _logTrend(newRecommendation, stat10m, stat2h);
        }

        #region Strategies

        /// <summary>
        /// Takes advantage of horizotal movements of the market
        /// </summary>
        /// <param name="stat3s">Stats 3 seconds</param>
        /// <param name="stat15s">stats 15 seconds</param>
        /// <param name="stat2m">Stats 2 minutes</param>
        /// <param name="stat10m">Stats 10 minutes</param>
        /// <param name="stat2h">Stats 2 hours</param>
        /// <param name="lowerLimitMA1h">Lower limit of moving average for 10 mintues</param>
        /// <param name="upperLimitMA1h">Upper limit of moving average for 10 minutes</param>
        /// <returns></returns>
        public datalayer.Enums.Action NormalSellStrategy(Stats3s stat3s, Stats15s stat15s, Stats2m stat2m, Stats10m stat10m, Stats2h stat2h, decimal lowerLimitMA1h, decimal upperLimitMA1h)
        {
            #region Argument checks

            if (stat3s == null || stat15s == null || stat2m == null || stat10m == null)
            {
                this._logger.Warning("Stats are NULL");
                return datalayer.Enums.Action.Hold;
            }

            #endregion

            var action = datalayer.Enums.Action.Hold;

            var lastCrossing = this._buffer.GetLatest10mAnd30mCrossing(stat3s.Symbol);

            // if moving average 1h is greater than moving average 3h
            // or moving average 1h crossed moving average 3h within the last 6 minutes
            if (stat10m.MovingAverage30m > stat10m.MovingAverage1h
                || (lastCrossing?.EventTime > DateTime.UtcNow.AddMinutes(-6)))
            {
                // Strong sell
                action = datalayer.Enums.Action.Sell;
            }

            // Check tendency
            if (stat2m.Slope10m > 0.002M
                && stat10m.Slope30m > 0.001M)
            {
                action = datalayer.Enums.Action.Hold;
            }

            return action;
        }

        /// <summary>
        /// Takes advantage of horizontal movement of the market
        /// </summary>
        /// <param name="stat3s">Stats 3 seconds</param>
        /// <param name="stat15s">stats 15 seconds</param>
        /// <param name="stat2m">Stats 2 minutes</param>
        /// <param name="stat10m">Stats 10 minutes</param>
        /// <param name="stat2h">Stats 2 hours</param>
        /// <param name="lowerLimitMA1h">Lower limit of moving average of 1 hour</param>
        /// <param name="upperLimitMA1h">Upper limit of moving average of 1 hour</param>
        /// <param name="distanceOkAndTrendPositive">Distance is OK and trend is positive</param>
        /// <returns></returns>
        public datalayer.Enums.Action NormalBuyStrategy(Stats3s stat3s, Stats15s stat15s, Stats2m stat2m, Stats10m stat10m, Stats2h stat2h, decimal lowerLimitMA1h, decimal upperLimitMA1h, bool distanceOkAndTrendPositive)
        {
            #region Argument checks

            if (stat3s == null || stat15s == null || stat2m == null || stat10m == null)
            {
                this._logger.Warning("Stats are NULL");
                return datalayer.Enums.Action.Hold;
            }

            #endregion

            var action = datalayer.Enums.Action.Hold;

            var lastCrossing = this._buffer.GetLatest10mAnd30mCrossing(stat3s.Symbol);

            // if moving average 1h is smaller than moving average 3h
            // or moving average 1h crossed moving average 3h within the last 6 minutes
            if (stat10m.MovingAverage30m < stat10m.MovingAverage1h
                || (lastCrossing?.EventTime > DateTime.UtcNow.AddMinutes(-6))
                || distanceOkAndTrendPositive)
            {
                // Strong sell
                action = datalayer.Enums.Action.Buy;
            }

            // Check tendency
            if (stat2m.Slope10m < -0.002M
                && stat10m.Slope30m < -0.001M)
            {
                action = datalayer.Enums.Action.Hold;
            }

            return action;
        }

        /// <summary>
        /// Takes advantage of vertical movement of the market
        /// </summary>
        /// <param name="stat3s">Stats 3 seconds</param>
        /// <param name="stat15s">stats 15 seconds</param>
        /// <param name="stat2m">Stats 2 minutes</param>
        /// <param name="stat10m">Stats 10 minutes</param>
        /// <param name="stat2h">Stats 2 hours</param>
        /// <param name="lowerLimitMA1h">Lower limit of moving average for 10 mintues</param>
        /// <param name="upperLimitMA1h">Upper limit of moving average for 10 minutes</param>
        /// <returns></returns>
        public datalayer.Enums.Action StrongSellStrategy(Stats3s stat3s, Stats15s stat15s, Stats2m stat2m, Stats10m stat10m, Stats2h stat2h, decimal lowerLimitMA1h, decimal upperLimitMA1h)
        {
            #region Argument checks

            if (stat3s == null || stat15s == null || stat2m == null || stat10m == null)
            {
                this._logger.Warning("Stats are NULL");
                return datalayer.Enums.Action.Hold;
            }

            #endregion

            return datalayer.Enums.Action.StrongSell;
        }

        /// <summary>
        /// Takes advantage of vertical movement of the market
        /// </summary>
        /// <param name="stat3s">Stats 3 seconds</param>
        /// <param name="stat15s">stats 15 seconds</param>
        /// <param name="stat2m">Stats 2 minutes</param>
        /// <param name="stat10m">Stats 10 minutes</param>
        /// <param name="stat2h">Stats 2 hours</param>
        /// <param name="lowerLimitMA1h">Lower limit of moving average of 1 hour</param>
        /// <param name="upperLimitMA1h">Upper limit of moving average of 1 hour</param>
        /// <param name="distanceOkAndTrendPositive">Distance is OK and trend is positive</param>
        /// <returns></returns>
        public datalayer.Enums.Action StrongBuyStrategy(Stats3s stat3s, Stats15s stat15s, Stats2m stat2m, Stats10m stat10m, Stats2h stat2h, decimal lowerLimitMA1h, decimal upperLimitMA1h, bool distanceOkAndTrendPositive)
        {
            #region Argument checks

            if (stat3s == null || stat15s == null || stat2m == null || stat10m == null)
            {
                this._logger.Warning("Stats are NULL");
                return datalayer.Enums.Action.Hold;
            }

            #endregion

            // Advise to sell
            var action = datalayer.Enums.Action.Hold;

            var lastCrossing = this._buffer.GetLatest10mAnd30mCrossing(stat3s.Symbol);

            // Check if funds are available
            bool fundsAvailable = false;
            var usdt = this._accountant.GetBalance("USDT").Result;
            if (usdt != null)
            {
                if (usdt.Free > 200)
                {
                    fundsAvailable = true;
                }
            }

            // Strong buy when crossing
            if (stat10m.MovingAverage30m < stat10m.MovingAverage1h
                || lowerLimitMA1h < stat10m.MovingAverage30m && stat10m.MovingAverage1h < upperLimitMA1h && stat2m.Slope5m > 0
                || lastCrossing?.EventTime > DateTime.UtcNow.AddMinutes(-6) && stat2m.Slope5m > 0
                || distanceOkAndTrendPositive
                || (fundsAvailable && stat2m.Slope15m > 0.015M && stat10m.Slope30m > 0.005M && stat10m.Slope1h > 0.0035M))
            {
                action = datalayer.Enums.Action.StrongBuy;
            }

            return action;
        }

        #endregion

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
        public void Finish()
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

                this._buffer.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
