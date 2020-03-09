using System.Threading;
using System.Threading.Tasks;

namespace trape.cli.collector.Jobs
{
    public abstract class AbstractJob
    {
        public abstract Task Execute(CancellationToken cancellationToken);
    }
}
