using System;
using System.Collections.Generic;

namespace Trape.Jobs
{
    /// <summary>
    /// Job Manager
    /// </summary>
    public class JobManager : IJobManager
    {
        #region Fields

        /// <summary>
        /// Schedulers
        /// </summary>
        private readonly List<JobScheduler> _schedulers;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>JobManager</c> class.
        /// </summary>
        public JobManager()
        {
            this._schedulers = new List<JobScheduler>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts a job
        /// </summary>
        /// <param name="job"></param>
        public void Start(IJob job)
        {
            #region Argument checks

            _ = job ?? throw new ArgumentNullException(paramName: nameof(job));

            #endregion

            var scheduler = new JobScheduler(job);
            this._schedulers.Add(scheduler);
            scheduler.Start();
        }

        /// <summary>
        /// Starts all registered jobs
        /// </summary>
        public void StartAll()
        {
            // Start all jobs
            foreach (var job in this._schedulers)
            {
                job.Start();
            }
        }

        /// <summary>
        /// Stops all registered jobs
        /// </summary>
        public void StopAll()
        {
            // Start all jobs
            foreach (var job in this._schedulers)
            {
                job.Stop();
            }
        }

        /// <summary>
        /// Terminates all jobs
        /// </summary>
        public void TerminateAll()
        {
            // Terminate all jobs
            foreach (var job in this._schedulers)
            {
                job.Stop();
                job.Terminate();
            }

            this._schedulers.Clear();
        }

        #endregion
    }
}
