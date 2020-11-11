using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trape.Jobs
{
    /// <summary>
    /// Wrapper for a job
    /// </summary>
    public class Job
    {
        #region Fields

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

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Job</c> class.
        /// </summary>
        /// <param name="executionInterval">Execution interval</param>
        /// <param name="action">Action to execute</param>
        public Job(TimeSpan executionInterval, Action action)
        {
            Setup(executionInterval, action, new CancellationToken());
        }


        /// <summary>
        /// Initializes a new instance of the <c>Job</c> class.
        /// </summary>
        /// <param name="executionInterval">Execution interval</param>
        /// <param name="action">Action to execute</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public Job(TimeSpan executionInterval, Action action, CancellationToken cancellationToken = default)
        {
            Setup(executionInterval, action, cancellationToken);
        }

        /// <summary>
        /// Actual setting up
        /// </summary>
        /// <param name="executionInterval"></param>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        private void Setup(TimeSpan executionInterval, Action action, CancellationToken cancellationToken = default)
        {
            #region Argument checks

            _action = action ?? throw new ArgumentNullException(paramName: nameof(action));

            #endregion

            _disposed = false;
            ExecutionInterval = executionInterval;
            _cancellationToken = cancellationToken;
            _synchronizer = new SemaphoreSlim(1, 1);
            _running = new SemaphoreSlim(1, 1);

            _timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = executionInterval.TotalMilliseconds
            };
            _timer.Elapsed += Timer_Elapsed;
        }

        #endregion

        #region Properties

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
                return _timer.Enabled;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Execute the action
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_synchronizer.CurrentCount == 0)
            {
                return;
            }
            _synchronizer.WaitAsync();

            try
            {
                _cancellationToken.ThrowIfCancellationRequested();

                _action.Invoke();
            }
            catch
            {
                throw;
            }
            finally
            {
                _synchronizer.Release();
            }
        }

        /// <summary>
        /// Start job
        /// </summary>
        public void Start()
        {
            // Start timer
            _timer.Start();

            // Enter running state
            _running.Wait();
        }

        /// <summary>
        /// Returns whether the timer is running or not
        /// </summary>
        /// <returns></returns>
        public bool IsRunning() => _timer.Enabled;

        /// <summary>
        /// Stop job
        /// </summary>
        public void Terminate()
        {
            // Block synchronizer
            _synchronizer.Wait();

            // Stop timer
            _timer.Stop();

            // Release
            _running.Release();
        }

        /// <summary>
        /// Returns a Task to wait for until job has finished
        /// </summary>
        /// <returns></returns>
        public Task WaitFor(CancellationToken stoppingToken)
        {
            // Wait for next time to enter, happens when task terminates
            return _running.WaitAsync(stoppingToken);
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
                _timer.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
