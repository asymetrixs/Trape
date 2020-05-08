using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.datalayer;
using trape.jobs;

namespace trape.cli.collector
{
    [Job(0, 5, 0)]
    class CleanUp : IJob
    {
        /// <summary>
        /// Runs a database clean up job that clean book tick records from the binance_book_tick table
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Execute(CancellationToken cancellationToken = default)
        {
            var logger = Program.Container.GetRequiredService<ILogger>().ForContext<CleanUp>();
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    var deletedRows = await database.CleanUpBookTicks(cancellationToken).ConfigureAwait(false);
                    logger.Debug($"Deleted {deletedRows} rows");
                }
                catch (Exception e)
                {
                    logger.Error(e.Message, e);
                }
            }
        }
    }
}
