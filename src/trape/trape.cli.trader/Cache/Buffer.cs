using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using trape.cli.trader.Cache.Models;

namespace trape.cli.trader.Cache
{
    public class Buffer : IBuffer, IDisposable
    {
        #region Fields

        private ILogger _logger;

        private bool _disposed;

        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        #region Timers

        private Timer _timerStats3s;

        private Timer _timerStats15s;

        private Timer _timerStats2m;

        private Timer _timerStats10m;

        private Timer _timerStats2h;

        private Timer _timerCurrentPrice;

        #endregion

        #region Stats

        public IEnumerable<Stats3s> Stats3s { get; private set; }

        public IEnumerable<Stats15s> Stats15s { get; private set; }

        public IEnumerable<Stats2m> Stats2m { get; private set; }

        public IEnumerable<Stats10m> Stats10m { get; private set; }

        public IEnumerable<Stats2h> Stats2h { get; private set; }

        public IEnumerable<CurrentPrice> CurrentPrices { get; private set; }

        #endregion

        #endregion

        public Buffer(ILogger logger)
        {
            if (null == logger)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._disposed = false;

            #region Timer setup

            this._timerStats3s = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 1).TotalMilliseconds
            };
            this._timerStats3s.Elapsed += _timerTrend3Seconds_Elapsed;


            this._timerStats15s = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 5).TotalMilliseconds
            };
            this._timerStats15s.Elapsed += _timerTrend15Seconds_Elapsed;

            this._timerStats2m = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 1, 0).TotalMilliseconds
            };
            this._timerStats2m.Elapsed += _timerTrend2Minutes_Elapsed;

            this._timerStats10m = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 3, 0).TotalMilliseconds
            };
            this._timerStats10m.Elapsed += _timerTrend10Minutes_Elapsed;

            this._timerStats2h = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 10, 0).TotalMilliseconds
            };
            this._timerStats2h.Elapsed += _timerTrend2Hours_Elapsed;

            this._timerCurrentPrice = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 1).TotalMilliseconds
            };
            this._timerCurrentPrice.Elapsed += _timerCurrentPrice_Elapsed;

            #endregion
        }

        #region Timer elapsed

        private async void _timerCurrentPrice_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.CurrentPrices = await database.GetCurrentPrice(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);
            database = null;

            this._logger.Verbose("Updated current price");
        }

        private async void _timerTrend3Seconds_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats3s = await database.Get3SecondsTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);
            database = null;

            this._logger.Verbose("Updated 3 seconds trend");
        }

        private async void _timerTrend15Seconds_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats15s = await database.Get15SecondsTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);
            database = null;

            this._logger.Verbose("Updated 15 seconds trend");
        }

        private async void _timerTrend2Minutes_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats2m = await database.Get2MinutesTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);
            database = null;

            this._logger.Verbose("Updated 2 minutes trend");
        }

        private async void _timerTrend10Minutes_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats10m = await database.Get10MinutesTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);
            database = null;

            this._logger.Verbose("Updated 10 minutes trend");
        }

        private async void _timerTrend2Hours_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats2h = await database.Get2HoursTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);
            database = null;

            this._logger.Verbose("Updated 2 hours trend");
        }

        #endregion

        public IEnumerable<string> GetSymbols()
        {
            // Take symbols that are in the widest spanning trend
            if (null == this.Stats2h)
            {
                return new List<string>();
            }

            return this.Stats2h.Where(t => t.IsValid()).Select(t => t.Symbol);
        }

        #region Start / Stop

        public async Task Start()
        {
            this._logger.Information("Starting Buffer");

            // Initial loading
            this._logger.Debug("Preloading buffer");
            var database = Pool.DatabasePool.Get();
            this.Stats3s = await database.Get3SecondsTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Stats15s = await database.Get15SecondsTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Stats2m = await database.Get2MinutesTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Stats10m = await database.Get10MinutesTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Stats2h = await database.Get2HoursTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.CurrentPrices = await database.GetCurrentPrice(this._cancellationTokenSource.Token).ConfigureAwait(true);

            Pool.DatabasePool.Put(database);
            database = null;

            this._logger.Debug("Buffer preloaded");

            // Starting of timers
            this._timerStats3s.Start();
            this._timerStats15s.Start();
            this._timerStats2m.Start();
            this._timerStats10m.Start();
            this._timerStats2h.Start();
            this._timerCurrentPrice.Start();

            this._logger.Information("Buffer started");
        }

        public void Stop()
        {
            this._logger.Information("Stopping buffer");

            this._cancellationTokenSource.Cancel();

            this._timerStats3s.Stop();
            this._timerStats15s.Stop();
            this._timerStats2m.Stop();
            this._timerStats10m.Stop();
            this._timerStats2h.Stop();
            this._timerCurrentPrice.Stop();

            this._logger.Information("Buffer stopped");
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
                this._timerStats3s.Dispose();
                this._timerStats15s.Dispose();
                this._timerStats2m.Dispose();
                this._timerStats10m.Dispose();
                this._timerStats2h.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
