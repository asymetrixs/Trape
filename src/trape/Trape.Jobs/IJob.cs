namespace Trape.Jobs
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IJob
    {
        /// <summary>
        /// Task to be executed
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task Execute(CancellationToken cancellationToken = default);
    }
}
