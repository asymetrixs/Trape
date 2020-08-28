using System;
using System.Threading;

namespace trape.jobs
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

            this._action = action ?? throw new ArgumentNullException(paramName: nameof(action));

            #endregion

            this._disposed = false;
            this.ExecutionInterval = executionInterval;
            this._cancellationToken = cancellationToken;
            this._synchronizer = new SemaphoreSlim(1, 1);

            this._timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = executionInterval.TotalMilliseconds
            };
            this._timer.Elapsed += _timer_Elapsed;
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
                return this._timer.Enabled;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Execute the action
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
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

        /// <summary>
        /// Start job
        /// </summary>
        public void Start()
        {
            this._timer.Start();
        }

        /// <summary>
        /// Returns whether the timer is running or not
        /// </summary>
        /// <returns></returns>
        public bool IsRunning() => this._timer.Enabled;

        /// <summary>
        /// Stop job
        /// </summary>
        public void Terminate()
        {
            // block synchronizer
            this._synchronizer.Wait();

            this._timer.Stop();
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
                this._timer.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
