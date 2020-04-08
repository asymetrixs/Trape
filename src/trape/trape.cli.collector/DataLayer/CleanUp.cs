using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using trape.jobs;

namespace trape.cli.collector.DataLayer
{
#if DEBUG
    [Job(0, 1, 0)]
#else
    [Job(0, 5, 0)]
#endif
    class CleanUp : IJob
    {
        /// <summary>
        /// Runs a database clean up job that clean book tick records from the binance_book_tick table
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Execute(CancellationToken cancellationToken)
        {
            var logger = Program.Services.GetRequiredService<ILogger>().ForContext<CleanUp>();
            var database = Pool.DatabasePool.Get();
            var deletedRows = await database.CleanUpBookTicks(cancellationToken).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);
            database = null;
            logger.Debug($"Cleaned up {deletedRows} book tick records");
        }
    }
}
