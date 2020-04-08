using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
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
        private ILogger _logger;

        /// <summary>
        /// Buffer
        /// </summary>
        private IBuffer _buffer;

        /// <summary>
        /// Cancellation Token
        /// </summary>
        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Timer when Analyst makes a new decision
        /// </summary>
        private Timer _timerRecommendationMaker;

        /// <summary>
        /// The last recommendation for an symbol
        /// </summary>
        private Dictionary<string, Recommendation> _lastRecommendation;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

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

            // Set up timer that makes decisions, every second
            this._timerRecommendationMaker = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 1).TotalMilliseconds
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
        private async void _makeRecommendation_Elapsed(object sender, ElapsedEventArgs e)
        {
            this._logger.Verbose("Calculating recommendation");

            foreach (var symbol in this._buffer.GetSymbols())
            {
                await _recommend(symbol).ConfigureAwait(false);
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
            if (null == stat3s || !stat3s.IsValid()
                || null == stat15s || !stat15s.IsValid()
                || null == stat2m || !stat2m.IsValid()
                || null == stat10m || !stat10m.IsValid()
                || null == stat2h || !stat2h.IsValid())
            {
                this._logger.Warning($"Skipped {symbol} due to old or incomplete data");
                this._lastRecommendation.Remove(symbol);
                return;
            }

            var database = Pool.DatabasePool.Get();

            // Get current symbol price
            var currentPrice = await database.GetCurrentPriceAsync(symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);

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
            //// MovingAverage2h about to cross MovingAverage6h and tendency is positive
            //else if (stat10m.Slope30m > 0
            //    && stat10m.Slope1h > 0
            //    && stat10m.Slope2h > 0
            //    && lowerLimit < stat2h.MovingAverage6h && stat2h.MovingAverage6h < upperLimit)
            //{
            //    action = Action.StrongBuy;
            //}
            // Sell
            else if (stat10m.Slope30m < 0
                && stat10m.Slope1h < 0
                && stat10m.Slope2h < 0.003M
                && stat10m.MovingAverage2h > stat2h.MovingAverage6h)
            {
                // sell
                action = Action.Sell;
            }
            //// MovingAverage2h about to cross MovingAverage6h and tendency is negative
            //else if (stat10m.Slope30m < 0
            //    && stat10m.Slope1h < 0
            //    && stat10m.Slope2h < 0
            //    && lowerLimit < stat2h.MovingAverage6h && stat2h.MovingAverage6h < upperLimit)
            //{
            //    action = Action.StrongSell;
            //}
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
            Pool.DatabasePool.Put(database);
            database = null;

            // Announce the trend every 5 seconds not to spam the log
            if (DateTime.UtcNow.Second % 5 == 0 || newRecommendation.Action != lastRecommendation?.Action)
            {
                this._logger.Verbose(_GetTrend(newRecommendation, stat10m, stat2h));
            }
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
