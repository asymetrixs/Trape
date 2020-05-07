using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.jobs;
using trape.datalayer;
using Microsoft.EntityFrameworkCore;

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
            var logger = Program.Services.GetRequiredService<ILogger>().ForContext<CleanUp>();
            var database = new TrapeContext(Program.Services.GetService<DbContextOptions<TrapeContext>>(), Program.Services.GetService<ILogger>());
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
