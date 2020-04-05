using System;
using System.Threading.Tasks;

namespace trape.cli.trader.Trading
{
    public interface ITradingTeam : IDisposable
    {
        void Start();

        Task Finish();
    }
}