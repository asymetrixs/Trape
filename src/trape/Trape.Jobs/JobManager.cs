namespace Trape.Jobs
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Job Manager
    /// </summary>
    public class JobManager : IJobManager
    {
        /// <summary>
        /// Schedulers
        /// </summary>
        private readonly List<JobScheduler> _schedulers;

        /// <summary>
        /// Initializes a new instance of the <c>JobManager</c> class.
        /// </summary>
        public JobManager()
        {
            this._schedulers = new List<JobScheduler>();
        }

        /// <summary>
        /// Starts a job
        /// </summary>
        /// <param name="job">Job</param>
        public void Start(IJob job)
        {
            _ = job ?? throw new ArgumentNullException(paramName: nameof(job));

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
    }
}
