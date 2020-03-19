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
        public async Task Execute(CancellationToken cancellationToken)
        {
            var logger = Program.Services.GetRequiredService<ILogger>();
            var database = Pool.DatabasePool.Get();
            var deletedRows = await database.CleanUpBookTicks(cancellationToken).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);
            database = null;
            logger.Debug($"Cleaned up {deletedRows} book tick records");
        }
    }
}
