using System;
using System.Threading.Tasks;

namespace trape.cli.trader.Trading
{
    public interface IBroker : IDisposable
    {
        string Symbol { get; }

        void Start(string symbolToTrade);

        Task Finish();
    }
}
