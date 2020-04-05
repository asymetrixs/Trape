using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using trape.cli.trader.Cache;
using trape.cli.trader.Cache.Models;

namespace trape.cli.trader.Analyze
{
    public class Analyst : IAnalyst
    {
        #region Fields

        private ILogger _logger;

        private IBuffer _buffer;

        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        private Timer _timerRecommender;

        private Dictionary<string, Recommendation> _lastRecommendation;

        private bool _disposed;

        #endregion

        #region Constructor

        public Analyst(ILogger logger, IBuffer buffer)
        {
            if (null == logger || null == buffer)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger.ForContext<Analyst>();
            this._buffer = buffer;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._lastRecommendation = new Dictionary<string, Recommendation>();
            this._disposed = false;

            this._timerRecommender = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 1).TotalMilliseconds
            };
            this._timerRecommender.Elapsed += _makeDecision_Elapsed;
        }

        #endregion

        private async void _makeDecision_Elapsed(object sender, ElapsedEventArgs e)
        {
            this._logger.Verbose("Calculating recommendation");

            foreach (var symbol in this._buffer.GetSymbols())
            {
                await _decide(symbol).ConfigureAwait(false);
            }
        }

        private async System.Threading.Tasks.Task _decide(string symbol)
        {
            if(string.IsNullOrEmpty(symbol))
            {
                return;
            }

            var s3s = this._buffer.Stats3s;
            var s15s = this._buffer.Stats15s;
            var s2m = this._buffer.Stats2m;
            var s10m = this._buffer.Stats10m;
            var s2h = this._buffer.Stats2h;
            var cp = this._buffer.CurrentPrices;

            var stat3s = s3s.SingleOrDefault(t => t.Symbol == symbol);
            var stat15s = s15s.SingleOrDefault(t => t.Symbol == symbol);
            var stat2m = s2m.SingleOrDefault(t => t.Symbol == symbol);
            var stat10m = s10m.SingleOrDefault(t => t.Symbol == symbol);
            var stat2h = s2h.SingleOrDefault(t => t.Symbol == symbol);

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
            var currentPrice = await database.GetCurrentPriceAsync(symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);

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
            else if (stat10m.Slope30m < 0
                && stat10m.Slope1h < 0.003M
                && stat10m.MovingAverage1h < stat10m.MovingAverage2h)
            {
                // sell
                action = Action.Sell;
            }
            else
            {
                action = Action.Wait;
            }

            var newRecommendation = new Recommendation()
            {
                Action = action,
                Price = currentPrice,
                Symbol = symbol
            };

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

            await database.InsertAsync(newRecommendation, stat3s, stat15s, stat2m, stat10m, stat2h, this._cancellationTokenSource.Token).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);
            database = null;

            if (DateTime.UtcNow.Second % 5 == 0 || newRecommendation.Action != lastRecommendation?.Action)
            {
                this._logger.Verbose(_GetTrend(newRecommendation, stat10m, stat2h));
            }
        }

        private static string _GetTrend(Recommendation recommendation, Stats10m stat10m, Stats2h stat2Hours)
        {
            var reco = recommendation.Action == Action.Buy ? "Buy :" : recommendation.Action.ToString();

            return $"{recommendation.Symbol}: {reco} | S1h: {Math.Round(stat10m.Slope1h, 4)} | S2h: {Math.Round(stat10m.Slope2h, 4)} | MA1h: {Math.Round(stat10m.MovingAverage1h, 4)} | MA2h: {Math.Round(stat10m.MovingAverage2h, 4)} | MA6h: {Math.Round(stat2Hours.MovingAverage6h, 4)}";
        }

        public Recommendation GetRecommendation(string symbol)
        {
            if (this._lastRecommendation.TryGetValue(symbol, out Recommendation decision))
            {
                return decision;
            }

            return null;
        }


        #region Start / Stop

        public void Start()
        {
            this._timerRecommender.Start();

            this._logger.Information("Recommender started");
        }

        public void Finish()
        {
            this._timerRecommender.Stop();

            this._cancellationTokenSource.Cancel();

            this._logger.Information("Recommender stopped");
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
