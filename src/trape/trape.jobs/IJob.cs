using System.Threading;
using System.Threading.Tasks;

namespace trape.jobs
{
    public interface IJob
    {
        Task Execute(CancellationToken cancellationToken);
    }
}
