using Serilog;
using System;
using System.Collections.Generic;
using trape.jobs;

namespace trape.cli.trader.WatchDog
{
    public class Checker : IChecker, IActive
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
        /// Initializes a new instance of the <c>Checker</c> class.
        /// </summary>
        public Checker(ILogger logger)
        {
            #region Argument checks

            if (logger == null)
            {
                throw new ArgumentNullException(paramName: nameof(logger));
            }

            #endregion

            this._logger = logger.ForContext(typeof(Checker));
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

            this._logger.Information($"Added: {watchMe.GetType().BaseType}");
        }

        /// <summary>
        /// Removes an instance from the watcher
        /// </summary>
        /// <param name="watchMe"></param>
        public void Remove(IActive watchMe)
        {
            this._watching.Remove(watchMe);

            this._logger.Information($"Removed: {watchMe.GetType().BaseType}");
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
                this._logger.Verbose($"Checking: {watchMe.GetType().BaseType}");

                if (watchMe.LastActive < DateTime.UtcNow.AddMinutes(-1))
                {
                    this._logger.Fatal($"Instance inactive: {watchMe.GetType()} {watchMe.GetType().BaseType}");
                    this._logger.Error($"Hard reset initiated");

                    // Hard exit
                    Environment.Exit(3);
                }
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
