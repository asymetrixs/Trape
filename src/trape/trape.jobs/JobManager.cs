using System;
using System.Collections.Generic;

namespace trape.jobs
{
    public class JobManager : IJobManager
    {
        private List<JobScheduler> _schedulers;

        public JobManager()
        {
            _schedulers = new List<JobScheduler>();
        }

        public void Start(IJob job)
        {
            if (null == job)
            {
                throw new ArgumentNullException("Paramter cannot be NULL");
            }

            var scheduler = new JobScheduler(job);
            this._schedulers.Add(scheduler);
            scheduler.Start();
        }

        public void StartAll()
        {
            // Start all jobs
            foreach (var job in _schedulers)
            {
                job.Start();
            }
        }

        public void StopAll()
        {
            // Start all jobs
            foreach (var job in _schedulers)
            {
                job.Stop();
            }
        }

        public void TerminateAll()
        {
            // Terminate all jobs
            foreach (var job in _schedulers)
            {
                job.Stop();
                job.Terminate();
            }
        }
    }
}
