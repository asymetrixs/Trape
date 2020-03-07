using binance.cli.DataCollection;
using binance.cli.Jobs;
using Binance.Net.Objects;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace binance.cli
{
    class Program
    {
        private static CancellationTokenSource cancellationToken;

        static async Task Main(string[] args)
        {
            cancellationToken = new CancellationTokenSource();

            Configuration.SetUp();
            Service.SetUp(cancellationToken.Token);

            var logger = Service.Get<ILogger>();
            logger.Information("Initialization complete");

            // Start trade info collection
            Service.Get<CollectionManager>().Run();
                        
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Client an");
                await Task.Delay(1000).ConfigureAwait(false);
            }
                        
            Console.ReadLine();

            Service.Get<CollectionManager>().Terminate();

            //JobManager.Stop();
            Console.WriteLine("JobManager stopped");
            //JobManager.Terminate();
            Console.WriteLine("JobManager terminated");
        }
    }
}
