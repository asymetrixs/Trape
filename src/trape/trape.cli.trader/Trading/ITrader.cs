using System;
using System.Threading.Tasks;

namespace trape.cli.trader.Trading
{
    public interface ITrader : IDisposable
    {
        string Symbol { get; }

        void Start(string symbolToTrade);

        Task Stop();
    }
}
