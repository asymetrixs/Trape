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

            var database = Pool.DatabasePool.Get();

            // Reference current buffer data
            var s3s = this._buffer.Stats3s;
            var s15s = this._buffer.Stats15s;
            var s2m = this._buffer.Stats2m;
            var s10m = this._buffer.Stats10m;
            var s2h = this._buffer.Stats2h;
            var cp = this._buffer.CurrentPrices;

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
                this._logger.Warning($"Skipped {symbol} due to old or incomplete data");
                this._lastRecommendation.Remove(symbol);
                return;
            }

            // Get current symbol price
            var currentPrice = this._buffer.GetBidPrice(symbol);

            decimal upperLimit = stat10m.MovingAverage2h * 1.001M;
            decimal lowerLimit = stat10m.MovingAverage2h * 0.999M;

            // Make the decision
            Action action;
            // Buy
            if (stat10m.Slope30m > 0
                && stat10m.Slope1h > 0
                && stat10m.Slope2h > -0.003M
                && stat10m.MovingAverage2h < stat2h.MovingAverage6h)
            {
                // buy
                action = Action.Buy;
            }
            // MovingAverage2h about to cross MovingAverage6h and tendency is positive
            else if (stat10m.Slope30m > 0.8M
                && stat10m.Slope1h > 0
                && stat10m.Slope2h > 0
                && lowerLimit < stat2h.MovingAverage6h && stat2h.MovingAverage6h < upperLimit)
            {
                action = Action.StrongBuy;
            }
            // Panic sell
            else if (stat3s.Slope5s < 0
                && stat3s.Slope10s < 0
                && stat3s.Slope15s < 0
                && stat3s.Slope30s < 0
                && stat15s.Slope45s < 0
                && stat15s.Slope1m < -1M
                && stat15s.Slope2m < -0.3M
                && stat15s.Slope3m < -0.15M
                && stat2m.Slope5m < -0M
                )
            {
                action = Action.PanicSell;
            }
            // Market seems to drop rapidly
            else if (stat15s.Slope1m < -0.8M
                && stat15s.Slope2m < -0.3M
                && stat15s.Slope3m < -0.15M
                && stat2m.Slope5m < -0.03M
                )
            {
                action = Action.StrongSell;
            }
            // Market drops normally
            else if (stat10m.Slope30m < 0
                && stat10m.Slope1h < 0
                && stat10m.Slope2h < 0.003M
                && stat10m.MovingAverage2h > stat2h.MovingAverage6h)
            {
                // sell
                action = Action.Sell;
            }
            else
            {
                // do nothng
                action = Action.Wait;
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
            if (DateTime.UtcNow.Second % 50 == 0 || newRecommendation.Action != lastRecommendation?.Action)
            {
                this._logger.Verbose(_GetTrend(newRecommendation, stat10m, stat2h));
            }
            // Return database
            Pool.DatabasePool.Put(database);
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
