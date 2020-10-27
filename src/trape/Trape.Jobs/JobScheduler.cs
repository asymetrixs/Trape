using System;
using System.Threading;

namespace trape.jobs
{
    /// <summary>
    /// Executes jobs regularly.
    /// </summary>
    class JobScheduler : IDisposable
    {
        #region Fields

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

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>JobScheduler</c> class.
        /// </summary>
        /// <param name="job"></param>
        internal JobScheduler(IJob job)
        {
            #region Argument checks

            _job = job ?? throw new ArgumentNullException(paramName: nameof(job));

            #endregion

            _disposed = false;
            _execute = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();

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
            _timer = new System.Timers.Timer
            {
                Interval = timeInterval.TotalMilliseconds,
                AutoReset = true
            };
            _timer.Elapsed += _timer_Elapsed;
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
            await _execute.WaitAsync().ConfigureAwait(false);

            try
            {
                await _job.Execute(_cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch
            {

            }
            finally
            {

            }

            _execute.Release();
        }

        #endregion

        #region Start / Stop / Terminate

        /// <summary>
        /// Starts the timer
        /// </summary>
        internal void Start()
        {
            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        internal void Stop()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }

        /// <summary>
        /// Terminates the task
        /// </summary>
        internal void Terminate()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }

                _timer.Stop();
                _timer.Dispose();

                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
