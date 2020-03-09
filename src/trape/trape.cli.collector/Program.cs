using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.collector.DataCollection;

namespace trape.cli.collector
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            Configuration.SetUp();
            Service.SetUp(cancellationTokenSource.Token);

            var logger = Service.Get<ILogger>();
            logger.Information("Initialization complete");

            // Start trade info collection
            Service.Get<ICollectionManager>().Run(cancellationTokenSource).ConfigureAwait(false);

            Console.ReadLine();

            Service.Get<ICollectionManager>().Terminate();

            //JobManager.Stop();
            Console.WriteLine("JobManager stopped");
            //JobManager.Terminate();
            Console.WriteLine("JobManager terminated");
        }
    }
}
