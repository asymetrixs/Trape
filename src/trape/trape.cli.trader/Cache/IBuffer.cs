using System.Threading.Tasks;

namespace trape.cli.trader.Cache
{
    public interface IBuffer
    {
        Task Start();

        void Stop();
    }
}
