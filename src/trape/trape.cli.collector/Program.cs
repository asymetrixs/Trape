using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.collector.DataCollection;

namespace trape.cli.collector
{
    class Program
    {
        private static CancellationTokenSource _cancellationToken;

        static async Task Main(string[] args)
        {
            _cancellationToken = new CancellationTokenSource();

            Configuration.SetUp();
            Service.SetUp(_cancellationToken.Token);

            var logger = Service.Get<ILogger>();
            logger.Information("Initialization complete");

            // Start trade info collection
            Service.Get<ICollectionManager>().Run().ConfigureAwait(false);

            Console.ReadLine();

            _cancellationToken.Cancel();

            Service.Get<ICollectionManager>().Terminate();

            //JobManager.Stop();
            Console.WriteLine("JobManager stopped");
            //JobManager.Terminate();
            Console.WriteLine("JobManager terminated");
        }
    }
}
