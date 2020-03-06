using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;


namespace binance.cli.Jobs
{
    class JobScheduler : IDisposable
    {
        private AbstractJob _job;

        private System.Timers.Timer _timer;

        private readonly SemaphoreSlim _execute;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private bool _disposed;

        internal JobScheduler(AbstractJob job)
        {
            if (null == job)
            {
                throw new ArgumentNullException("Job cannot be NULL.");
            }

            this._disposed = false;
            this._execute = new SemaphoreSlim(1, 1);
            this._job = job;
            this._cancellationTokenSource = new CancellationTokenSource();

            TimeSpan timeInterval = default;

            foreach (var attr in job.GetType().GetCustomAttributes(true))
            {
                if (attr is JobAttribute)
                {
                    var jobAttr = attr as JobAttribute;
                    timeInterval = jobAttr.Interval;
                }
            }

            if (timeInterval == default)
            {
                throw new InvalidOperationException("Job must have a valid interval configured.");
            }

            this._timer = new System.Timers.Timer
            {
                Interval = timeInterval.TotalMilliseconds,
                AutoReset = true
            };
            this._timer.Elapsed += _timer_Elapsed;
        }

        private async void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await this._execute.WaitAsync().ConfigureAwait(false);

            try
            {
                await this._job.Execute(this._cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch
            {

            }
            finally
            {
                
            }

            this._execute.Release();
        }

        internal void Start()
        {
            if (!this._timer.Enabled)
            {
                this._timer.Start();
            }
        }

        internal void Stop()
        {
            if (this._timer.Enabled)
            {
                this._timer.Stop();
            }
        }

        internal void Terminate()
        {
            if (!this._cancellationTokenSource.IsCancellationRequested)
            {
                this._cancellationTokenSource.Cancel();
            }
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
                if (!this._cancellationTokenSource.IsCancellationRequested)
                {
                    this._cancellationTokenSource.Cancel();
                }

                this._timer.Stop();
                this._timer.Dispose();

                this._cancellationTokenSource.Dispose();
            }

            this._disposed = true;
        }
    }
}
