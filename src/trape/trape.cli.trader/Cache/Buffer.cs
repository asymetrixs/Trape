using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using trape.cli.trader.Cache.Trends;
using trape.cli.trader.DataLayer;

namespace trape.cli.trader.Cache
{
    public class Buffer : IBuffer, IDisposable
    {
        private ILogger _logger;

        private bool _disposed;

        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        private Timer _timerTrend3Seconds;

        private Timer _timerTrend15Seconds;

        private Timer _timerTrend2Minutes;

        private Timer _timerTrend10Minutes;

        private Timer _timerTrend2Hours;

        public IEnumerable<Trend3Seconds> Trends3Seconds { get; private set; }

        public IEnumerable<Trend15Seconds> Trends15Seconds { get; private set; }

        public IEnumerable<Trend2Minutes> Trends2Minutes { get; private set; }

        public IEnumerable<Trend10Minutes> Trends10Minutes { get; private set; }

        public IEnumerable<Trend2Hours> Trends2Hours { get; private set; }


        public Buffer(ILogger logger)
        {
            if (null == logger)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._disposed = false;

            this._timerTrend3Seconds = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 3).TotalMilliseconds
            };
            this._timerTrend3Seconds.Elapsed += _timerTrend3Seconds_Elapsed;


            this._timerTrend15Seconds = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 15).TotalMilliseconds
            };
            this._timerTrend15Seconds.Elapsed += _timerTrend15Seconds_Elapsed;

            this._timerTrend2Minutes = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 2, 0).TotalMilliseconds
            };
            this._timerTrend2Minutes.Elapsed += _timerTrend2Minutes_Elapsed;

            this._timerTrend10Minutes = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 10, 0).TotalMilliseconds
            };
            this._timerTrend10Minutes.Elapsed += _timerTrend10Minutes_Elapsed;

            this._timerTrend2Hours = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(2, 0, 0).TotalMilliseconds
            };
            this._timerTrend2Hours.Elapsed += _timerTrend2Hours_Elapsed;
        }

        private async void _timerTrend3Seconds_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Program.Services.GetService(typeof(ITrapeContext)) as ITrapeContext;
            this.Trends3Seconds = await database.Get3SecondsTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);

            //this._logger.Debug("Updated 3 seconds trend");
        }

        private async void _timerTrend15Seconds_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Program.Services.GetService(typeof(ITrapeContext)) as ITrapeContext;
            this.Trends15Seconds = await database.Get15SecondsTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);

            //this._logger.Debug("Updated 15 seconds trend");
        }

        private async void _timerTrend2Minutes_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Program.Services.GetService(typeof(ITrapeContext)) as ITrapeContext;
            this.Trends2Minutes = await database.Get2MinutesTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);

            //this._logger.Debug("Updated 2 minutes trend");
        }

        private async void _timerTrend10Minutes_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Program.Services.GetService(typeof(ITrapeContext)) as ITrapeContext;
            this.Trends10Minutes = await database.Get10MinutesTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);

            //this._logger.Debug("Updated 10 minutes trend");
        }

        private async void _timerTrend2Hours_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Program.Services.GetService(typeof(ITrapeContext)) as ITrapeContext;
            this.Trends2Hours = await database.Get2HoursTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);

            //this._logger.Verbose("Updated 2 hours trend");
        }

        public async Task Start()
        {
            this._logger.Information("Starting Buffer");

            // Initial loading
            this._logger.Verbose("Preloading buffer");
            var database = Program.Services.GetService(typeof(ITrapeContext)) as ITrapeContext;
            this.Trends3Seconds = await database.Get3SecondsTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Trends15Seconds = await database.Get15SecondsTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Trends2Minutes = await database.Get2MinutesTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Trends10Minutes = await database.Get10MinutesTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Trends2Hours = await database.Get2HoursTrend(this._cancellationTokenSource.Token).ConfigureAwait(true);

            this._logger.Verbose("Buffer preloaded");

            // Starting of timers
            this._timerTrend3Seconds.Start();
            this._timerTrend15Seconds.Start();
            this._timerTrend2Minutes.Start();
            this._timerTrend10Minutes.Start();
            this._timerTrend2Hours.Start();

            this._logger.Information("Buffer started");
        }

        public void Stop()
        {
            this._logger.Information("Stopping buffer");

            this._cancellationTokenSource.Cancel();

            this._timerTrend3Seconds.Stop();
            this._timerTrend15Seconds.Stop();
            this._timerTrend2Minutes.Stop();
            this._timerTrend10Minutes.Stop();
            this._timerTrend2Hours.Stop();

            this._logger.Information("Buffer stopped");
        }

        public IEnumerable<string> GetSymbols()
        {
            // Take symbols that are in the widest spanning trend
            if (null == this.Trends2Hours)
            {
                return new List<string>();
            }

            return this.Trends2Hours.Where(t => t.IsValid()).Select(t => t.Symbol);
        }

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
                this._timerTrend3Seconds.Dispose();
                this._timerTrend15Seconds.Dispose();
                this._timerTrend2Minutes.Dispose();
                this._timerTrend10Minutes.Dispose();
                this._timerTrend2Hours.Dispose();
            }

            this._disposed = true;
        }
    }
}
