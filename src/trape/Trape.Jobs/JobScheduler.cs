namespace Trape.Jobs
{
    using System;
    using System.Threading;

    /// <summary>
    /// Executes jobs regularly.
    /// </summary>
    internal class JobScheduler : IDisposable
    {
        /// <summary>
        /// Job being executed
        /// </summary>
        private readonly IJob _job;

        /// <summary>
        /// Timer to execute job
        /// </summary>
        private readonly System.Timers.Timer _timer;

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

        /// <summary>
        /// Initializes a new instance of the <c>JobScheduler</c> class.
        /// </summary>
        /// <param name="job">Job</param>
        internal JobScheduler(IJob job)
        {
            this._job = job ?? throw new ArgumentNullException(paramName: nameof(job));

            this._disposed = false;
            this._execute = new SemaphoreSlim(1, 1);
            this._cancellationTokenSource = new CancellationTokenSource();

            TimeSpan timeInterval = default;

            // Find the interval
            foreach (var attr in job.GetType().GetCustomAttributes(true))
            {
                if (attr is JobAttribute jobAttr)
                {
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
                AutoReset = true,
            };
            this._timer.Elapsed += this.Timer_Elapsed;
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

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

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">Disposing</param>
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
                this._execute.Dispose();
            }

            this._disposed = true;
        }

        /// <summary>
        /// Execute the registered job
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        private async void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await this._execute.WaitAsync().ConfigureAwait(false);

            await this._job.Execute(this._cancellationTokenSource.Token).ConfigureAwait(false);

            this._execute.Release();
        }
    }
}
