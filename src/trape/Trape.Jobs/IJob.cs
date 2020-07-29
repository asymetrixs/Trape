using System.Threading;
using System.Threading.Tasks;

namespace trape.jobs
{
    public interface IJob
    {
        /// <summary>
        /// Task to be executed
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Execute(CancellationToken cancellationToken = default);
    }
}
