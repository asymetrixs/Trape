using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using trape.cli.trader.Cache;
using trape.cli.trader.Cache.Models;

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
        private readonly System.Timers.Timer _timerRecommendationMaker;

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
        private Dictionary<string, Strategy> _lastStrategy;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new instance of the <c>Analyst</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="buffer">Buffer</param>
        public Analyst(ILogger logger, IBuffer buffer)
        {
            if (logger == null || buffer == null)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger.ForContext<Analyst>();
            this._buffer = buffer;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._lastRecommendation = new Dictionary<string, Recommendation>();
            this._disposed = false;
            this._recommendationSynchronizer = new SemaphoreSlim(1, 1);
            this._lastStrategy = new Dictionary<string, Strategy>();

            // Set up timer that makes decisions, every second
            this._timerRecommendationMaker = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 0, 0, 100).TotalMilliseconds
            };
            this._timerRecommendationMaker.Elapsed += _makeRecommendation_Elapsed;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create a new decision for every symbol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _makeRecommendation_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
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
        private async System.Threading.Tasks.Task _recommend(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return;
            }

            // Placeholder for strategy
            if (!this._lastStrategy.ContainsKey(symbol))
            {
                this._lastStrategy.Add(symbol, Strategy.Hold);
            }

            var database = Pool.DatabasePool.Get();

            // Reference current buffer data
            var s3s = this._buffer.Stats3s;
            var s15s = this._buffer.Stats15s;
            var s2m = this._buffer.Stats2m;
            var s10m = this._buffer.Stats10m;
            var s2h = this._buffer.Stats2h;

            // Extract data for current symbol
            var stat3s = s3s.SingleOrDefault(t => t.Symbol == symbol);
            var stat15s = s15s.SingleOrDefault(t => t.Symbol == symbol);
            var stat2m = s2m.SingleOrDefault(t => t.Symbol == symbol);
            var stat10m = s10m.SingleOrDefault(t => t.Symbol == symbol);
            var stat2h = s2h.SingleOrDefault(t => t.Symbol == symbol);

            // Check that data is valud
            if (stat3s == null || !stat3s.IsValid()
                || stat15s == null || !stat15s.IsValid()
                || stat2m == null || !stat2m.IsValid()
                || stat10m == null || !stat10m.IsValid()
                || stat2h == null || !stat2h.IsValid())
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
            const decimal strongThreshold = 0.004M;

            // Make the decision
            var action = Action.Hold;
            Strategy strategy = Strategy.Hold;

            var lastFallingPrice = this._buffer.GetLastFallingPrice(symbol);

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
                action = Action.PanicSell;
                this._logger.Warning("Panic Mode");
                strategy = Strategy.PanicSell;
            }
            else if (stat10m.Slope1h > strongThreshold)
            {
                action = StrongBuyStrategy(stat3s, stat15s, stat2m, stat10m, stat2h, lowerLimitMA1h, upperLimitMA1h);
                strategy = Strategy.StrongBuy;
            }
            else if (stat10m.Slope1h > 0.0015M)
            {
                action = NormalBuyStrategy(stat3s, stat15s, stat2m, stat10m, stat2h, lowerLimitMA1h, upperLimitMA1h);
                strategy = Strategy.NormalBuy;
            }
            else if (stat10m.Slope1h < -strongThreshold)
            {
                action = StrongSellStrategy(stat3s, stat15s, stat2m, stat10m, stat2h, lowerLimitMA1h, upperLimitMA1h);
                strategy = Strategy.StrongSell;
            }
            else if (stat10m.Slope1h < -0.0015M)
            {
                action = NormalSellStrategy(stat3s, stat15s, stat2m, stat10m, stat2h, lowerLimitMA1h, upperLimitMA1h);
                strategy = Strategy.NormalSell;
            }

            var now = DateTime.UtcNow;
            // Print strategy changes
            if (this._lastStrategy[symbol] != strategy)
            {
                this._logger.Information($"{symbol} Switching stategy: {this._lastStrategy[symbol]} -> {strategy}");
                this._lastStrategy[symbol] = strategy;
            }
            // Print strategy every hour in log
            else if (now.Minute == 0 && now.Second == 0 && now.Millisecond < 100)
            {
                this._logger.Information($"{symbol} Stategy: {strategy}");
            }

            // Instantiate new recommendation
            var newRecommendation = new Recommendation()
            {
                Action = action,
                Price = currentPrice,
                Symbol = symbol,
                EventTime = DateTime.UtcNow
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

            // Store recommendation in database
            await database.InsertAsync(newRecommendation, stat3s, stat15s, stat2m, stat10m, stat2h, this._cancellationTokenSource.Token).ConfigureAwait(false);

            // Announce the trend every 5 seconds not to spam the log
            if (DateTime.UtcNow.Second % 5 == 0 || newRecommendation.Action != lastRecommendation?.Action)
            {
                this._logger.Verbose(_GetTrend(newRecommendation, stat10m, stat2h));
            }
            // Return database
            Pool.DatabasePool.Put(database);
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
        /// <param name="upperLimitMA1h">Upperl imit of moving average for 10 minutes</param>
        /// <returns></returns>
        public Action NormalSellStrategy(Stats3s stat3s, Stats15s stat15s, Stats2m stat2m, Stats10m stat10m, Stats2h stat2h, decimal lowerLimitMA1h, decimal upperLimitMA1h)
        {
            #region Argument checks

            if (stat3s == null || stat15s == null || stat2m == null || stat10m == null)
            {
                this._logger.Warning("Stats are NULL");
                return Action.Hold;
            }

            #endregion

            var action = Action.Hold;

            var lastCrossing = this._buffer.GetLatest1hAnd3hCrossing(stat3s.Symbol);

            // if moving average 1h is greater than moving average 3h
            // or moving average 1h crossed moving average 3h within the last 6 minutes
            if (stat10m.MovingAverage1h > stat10m.MovingAverage3h
                || (lastCrossing?.EventTime > DateTime.UtcNow.AddMinutes(-6)))
            {
                // Strong sell
                action = Action.Sell;
            }

            // Check tendency
            if (stat2m.Slope10m > 0.002M
                && stat10m.Slope30m > 0.001M)
            {
                action = Action.Hold;
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
        /// <param name="lowerLimitMA1h">Lower limit of moving average for 10 mintues</param>
        /// <param name="upperLimitMA1h">Upperl imit of moving average for 10 minutes</param>
        /// <returns></returns>
        public Action NormalBuyStrategy(Stats3s stat3s, Stats15s stat15s, Stats2m stat2m, Stats10m stat10m, Stats2h stat2h, decimal lowerLimitMA1h, decimal upperLimitMA1h)
        {
            #region Argument checks

            if (stat3s == null || stat15s == null || stat2m == null || stat10m == null)
            {
                this._logger.Warning("Stats are NULL");
                return Action.Hold;
            }

            #endregion

            var action = Action.Hold;

            var lastCrossing = this._buffer.GetLatest1hAnd3hCrossing(stat3s.Symbol);

            // if moving average 1h is smaller than moving average 3h
            // or moving average 1h crossed moving average 3h within the last 6 minutes
            if (stat10m.MovingAverage1h < stat10m.MovingAverage3h
                || (lastCrossing?.EventTime > DateTime.UtcNow.AddMinutes(-6)))
            {
                // Strong sell
                action = Action.Buy;
            }

            // Check tendency
            if (stat2m.Slope10m < -0.002M
                && stat10m.Slope30m < -0.001M)
            {
                action = Action.Hold;
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
        /// <param name="upperLimitMA1h">Upperl imit of moving average for 10 minutes</param>
        /// <returns></returns>
        public Action StrongSellStrategy(Stats3s stat3s, Stats15s stat15s, Stats2m stat2m, Stats10m stat10m, Stats2h stat2h, decimal lowerLimitMA1h, decimal upperLimitMA1h)
        {
            #region Argument checks

            if (stat3s == null || stat15s == null || stat2m == null || stat10m == null)
            {
                this._logger.Warning("Stats are NULL");
                return Action.Hold;
            }

            #endregion

            // Advise to sell
            var action = Action.Hold;

            if (stat10m.MovingAverage1h > stat10m.MovingAverage3h)
            {
                action = Action.StrongSell;
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
        /// <param name="upperLimitMA1h">Upperl imit of moving average for 10 minutes</param>
        /// <returns></returns>
        public Action StrongBuyStrategy(Stats3s stat3s, Stats15s stat15s, Stats2m stat2m, Stats10m stat10m, Stats2h stat2h, decimal lowerLimitMA1h, decimal upperLimitMA1h)
        {
            #region Argument checks

            if (stat3s == null || stat15s == null || stat2m == null || stat10m == null)
            {
                this._logger.Warning("Stats are NULL");
                return Action.Hold;
            }

            #endregion

            // Advise to sell
            var action = Action.Hold;

            var lastCrossing = this._buffer.GetLatest1hAnd3hCrossing(stat3s.Symbol);

            // Strong buy also when crossing
            if (stat10m.MovingAverage1h < stat10m.MovingAverage3h
                || lowerLimitMA1h < stat10m.MovingAverage3h && stat10m.MovingAverage3h < upperLimitMA1h
                || (lastCrossing?.EventTime > DateTime.UtcNow.AddMinutes(-6)))
            {
                action = Action.StrongBuy;
            }

            return action;
        }

        /// <summary>
        /// Returns a string representing current trend data
        /// </summary>
        /// <param name="recommendation">The recommendation that was calculated</param>
        /// <param name="stat10m">10 minutes stats</param>
        /// <param name="stat2Hours">2 hours stats</param>
        /// <returns>String that returns current trends</returns>
        private static string _GetTrend(Recommendation recommendation, Stats10m stat10m, Stats2h stat2Hours)
        {
            var reco = recommendation.Action == Action.Buy ? "Buy :" : recommendation.Action.ToString();

            return $"{recommendation.Symbol}: {reco} | S1h: {Math.Round(stat10m.Slope1h, 4)} | S2h: {Math.Round(stat10m.Slope2h, 4)} | MA1h: {Math.Round(stat10m.MovingAverage1h, 4)} | MA2h: {Math.Round(stat10m.MovingAverage2h, 4)} | MA6h: {Math.Round(stat2Hours.MovingAverage6h, 4)}";
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
                && decision.EventTime > DateTime.UtcNow.AddSeconds(-5))
            {
                return decision;
            }

            return null;
        }

        #endregion

        #endregion

        #region Start / Stop

        /// <summary>
        /// Starts the <c>Analyst</c> instance
        /// </summary>
        public void Start()
        {
            // Start recommendation maker
            this._timerRecommendationMaker.Start();

            this._logger.Information("Analyst started");
        }

        /// <summary>
        /// Stops the <c>Analyst</c> instance
        /// </summary>
        public void Finish()
        {
            // Stop recommendation maker
            this._timerRecommendationMaker.Stop();

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
