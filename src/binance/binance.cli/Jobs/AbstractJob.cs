using System.Threading;
using System.Threading.Tasks;

namespace binance.cli.Jobs
{
    public abstract class AbstractJob
    {
        public abstract Task Execute(CancellationToken cancellationToken);
    }
}
