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
    [Job(48, 0, 0)]
#endif
    class CleanUp : IJob
    {
        public async Task Execute(CancellationToken cancellationToken)
        {
            var database = Program.Services.GetRequiredService<ICoinTradeContext>();
            var logger = Program.Services.GetRequiredService<ILogger>();

            var deletedRows = await database.CleanUpBookTicks(cancellationToken).ConfigureAwait(false);

            logger.Information($"Cleaned up {deletedRows} book tick records");
        }
    }
}
