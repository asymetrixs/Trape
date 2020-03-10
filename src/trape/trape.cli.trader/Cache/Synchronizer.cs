using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace trape.cli.trader.Cache
{
    public class Synchronizer : ISynchronizer, IDisposable
    {
        private bool _disposed;

        private Buffer _buffer;

        private System.Threading.CancellationTokenSource _cancellationTokenSource;
        
        private Timer _timerTrend3Seconds;

        private Timer _timerTrend15Seconds;

        private Timer _timerTrend2Minutes;

        private Timer _timerTrend10Minutes;

        public Synchronizer(Buffer buffer)
        {
            if (null == buffer)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._buffer = buffer;
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

        }

        private void _timerTrend3Seconds_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _timerTrend15Seconds_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _timerTrend2Minutes_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _timerTrend10Minutes_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        

        public void Start()
        {
            this._timerTrend3Seconds.Start();
            this._timerTrend15Seconds.Start();
            this._timerTrend2Minutes.Start();
            this._timerTrend10Minutes.Start();
        }

        public void Stop()
        {
            this._timerTrend3Seconds.Stop();
            this._timerTrend15Seconds.Stop();
            this._timerTrend2Minutes.Stop();
            this._timerTrend10Minutes.Stop();
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
                this._3secTimer.Dispose();
            }

            this._disposed = true;
        }
    }
}
