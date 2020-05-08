using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using trape.jobs;

namespace trape.cli.trader.WatchDog
{
    public class Warden : IWarden, IActive
    {
        #region Fields

        /// <summary>
        /// List of objects watching
        /// </summary>
        private readonly List<IActive> _watching;

        /// <summary>
        /// Timer to check job
        /// </summary>
        private readonly Job _jobChecker;

        /// <summary>
        /// Last active
        /// </summary>
        public DateTime LastActive { get; private set; }

        /// <summary>
        /// Logger
        /// </summary>
        public readonly ILogger _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Warden</c> class.
        /// </summary>
        public Warden(ILogger logger)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            #endregion

            this._logger = logger.ForContext(typeof(Warden));
            this._watching = new List<IActive>();
            this._jobChecker = new Job(new TimeSpan(0, 0, 5), _check);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds an instance to watcher
        /// </summary>
        /// <param name="watchMe"></param>
        public void Add(IActive watchMe)
        {
            this._watching.Add(watchMe);

            this._logger.Information($"Added: {watchMe.GetType().Name}");
        }

        /// <summary>
        /// Removes an instance from the watcher
        /// </summary>
        /// <param name="watchMe"></param>
        public void Remove(IActive watchMe)
        {
            this._watching.Remove(watchMe);

            this._logger.Information($"Removed: {watchMe.GetType().Name}");
        }

        /// <summary>
        /// Perform checks
        /// </summary>
        private void _check()
        {
            this.LastActive = DateTime.UtcNow;

            // Check all objects
            foreach (var watchMe in this._watching)
            {
                this._logger.Verbose($"Checking: {watchMe.GetType().Name}");

                if (watchMe.LastActive < DateTime.UtcNow.AddMinutes(-1))
                {
                    this._logger.Fatal($"Instance inactive: {watchMe.GetType().Name}");
                    this._logger.Error($"Hard reset initiated");

                    // Hard exit
                    Environment.Exit(3);
                }
            }

            // Output once per hour
            if (DateTime.UtcNow.Minute == 0 && DateTime.UtcNow.Second < 5)
            {
                var jobs = string.Join(',', this._watching.Select(w => w.GetType().Name)).TrimEnd(',');
                this._logger.Information($"Checked {this._watching.Count} jobs: {jobs}");
            }
        }

        /// <summary>
        /// Starts the checker
        /// </summary>
        public void Start()
        {
            this._jobChecker.Start();

            this.Add(this);
        }

        /// <summary>
        /// Terminates the checker
        /// </summary>
        public void Terminate()
        {
            this._jobChecker.Terminate();

            this._watching.Clear();
        }

        #endregion

        // TODO: Extend IActive with signaling so that the manager gets informed about failing instances
    }
}
