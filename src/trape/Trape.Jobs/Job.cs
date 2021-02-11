namespace Trape.Jobs
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wrapper for a job
    /// </summary>
    public class Job : IDisposable
    {
        /// <summary>
        /// Actopm
        /// </summary>
        private Action _action;

        /// <summary>
        /// Cancellation Token
        /// </summary>
        private CancellationToken _cancellationToken;

        /// <summary>
        /// Timer
        /// </summary>
        private System.Timers.Timer _timer;

        /// <summary>
        /// Synchronizer
        /// </summary>
        private SemaphoreSlim _synchronizer;

        /// <summary>
        /// Dispose
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Token to wait for job to finish
        /// </summary>
        private SemaphoreSlim _running;

        /// <summary>
        /// Initializes a new instance of the <c>Job</c> class.
        /// </summary>
        /// <param name="executionInterval">Execution interval</param>
        /// <param name="action">Action to execute</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public Job(TimeSpan executionInterval, Action action, CancellationToken cancellationToken = default)
        {
            this._action = action ?? throw new ArgumentNullException(paramName: nameof(action));

            this._disposed = false;
            this.ExecutionInterval = executionInterval;
            this._cancellationToken = cancellationToken;
            this._synchronizer = new SemaphoreSlim(1, 1);
            this._running = new SemaphoreSlim(1, 1);

            this._timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = executionInterval.TotalMilliseconds,
            };
            this._timer.Elapsed += this.Timer_Elapsed;
        }

        /// <summary>
        /// Execution Interval
        /// </summary>
        public TimeSpan ExecutionInterval { get; private set; }

        /// <summary>
        /// Enabled
        /// </summary>
        public bool Enabled
        {
            get
            {
                return this._timer.Enabled;
            }
        }

        /// <summary>
        /// Start job
        /// </summary>
        public void Start()
        {
            // Start timer
            this._timer.Start();

            // Enter running state
            this._running.Wait();
        }

        /// <summary>
        /// Returns whether the timer is running or not
        /// </summary>
        public bool IsRunning() => this._timer.Enabled;

        /// <summary>
        /// Stop job
        /// </summary>
        public void Terminate()
        {
            // Block synchronizer
            this._synchronizer.Wait();

            // Stop timer
            this._timer.Stop();

            // Release
            this._running.Release();
        }

        /// <summary>
        /// Returns a Task to wait for until job has finished
        /// </summary>
        public Task WaitFor(CancellationToken stoppingToken)
        {
            // Wait for next time to enter, happens when task terminates
            return this._running.WaitAsync(stoppingToken);
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
                this._timer.Dispose();
                this._running.Dispose();
                this._timer.Dispose();
                this._synchronizer.Dispose();
            }

            this._disposed = true;
        }

        /// <summary>
        /// Execute the action
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this._synchronizer.CurrentCount == 0)
            {
                return;
            }

            this._synchronizer.WaitAsync();

            try
            {
                this._cancellationToken.ThrowIfCancellationRequested();

                this._action.Invoke();
            }
            catch
            {
                throw;
            }
            finally
            {
                this._synchronizer.Release();
            }
        }
    }
}
