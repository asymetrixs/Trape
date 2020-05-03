using System;
using System.Threading;

namespace trape.jobs
{
    class JobScheduler : IDisposable
    {
        #region Fields

        /// <summary>
        /// Job being executed
        /// </summary>
        private IJob _job;

        /// <summary>
        /// Timer to execute job
        /// </summary>
        private System.Timers.Timer _timer;

        /// <summary>
        /// Synchronization
        /// </summary>
        private readonly SemaphoreSlim _execute;

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        #endregion

        #region Constructor

        internal JobScheduler(IJob job)
        {
            #region Argument checks

            if (job == null)
            {
                throw new ArgumentNullException(paramName: nameof(job));
            }

            #endregion

            this._disposed = false;
            this._execute = new SemaphoreSlim(1, 1);
            this._job = job;
            this._cancellationTokenSource = new CancellationTokenSource();

            TimeSpan timeInterval = default;

            // Find the interval
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

            // Set up timer
            this._timer = new System.Timers.Timer
            {
                Interval = timeInterval.TotalMilliseconds,
                AutoReset = true
            };
            this._timer.Elapsed += _timer_Elapsed;
        }

        #endregion

        #region Timer Elapsed

        /// <summary>
        /// Execute the registered job
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        #endregion

        #region Start / Stop / Terminate

        /// <summary>
        /// Starts the timer
        /// </summary>
        internal void Start()
        {
            if (!this._timer.Enabled)
            {
                this._timer.Start();
            }
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        internal void Stop()
        {
            if (this._timer.Enabled)
            {
                this._timer.Stop();
            }
        }

        /// <summary>
        /// Terminates the task
        /// </summary>
        internal void Terminate()
        {
            if (!this._cancellationTokenSource.IsCancellationRequested)
            {
                this._cancellationTokenSource.Cancel();
            }
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

        #endregion
    }
}
