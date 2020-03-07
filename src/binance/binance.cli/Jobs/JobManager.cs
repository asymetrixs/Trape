using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace binance.cli.Jobs
{
    internal static class JobManager
    {
        private static List<JobScheduler> _schedulers;

        static JobManager()
        {
            _schedulers = new List<JobScheduler>();

            var symbols = Configuration.GetValue("binance:symbols");
            
            // Get all types in assembly
            foreach (var type in typeof(JobManager).Assembly.GetTypes())
            {
                // Filter for those with JobAttribute
                if (type.GetCustomAttributes(typeof(JobAttribute), true).Length > 0)
                {
                    // Check that it is an inherited type of AbstractJob
                    if(type.BaseType == typeof(AbstractJob))
                    {
                        // Instantiate an object of this class for each symbol
                        foreach (var symbol in symbols)
                        {
                            ConstructorInfo ctor = type.GetConstructor(new[] { typeof(string) });
                            object instance = ctor.Invoke(new object[] { symbol });
                            Console.WriteLine("Added " + instance.GetType().ToString());
                            _schedulers.Add(new JobScheduler(instance as AbstractJob));
                        }
                    }
                }
            }
        }

        internal static void Start()
        {
            // Start all jobs
            foreach (var job in _schedulers)
            {
                job.Start();
            }
        }

        internal static void Stop()
        {
            // Start all jobs
            foreach (var job in _schedulers)
            {
                job.Stop();
            }
        }

        internal static void Terminate()
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
