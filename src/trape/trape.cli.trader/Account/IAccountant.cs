using Binance.Net.Objects;
using System;
using System.Threading.Tasks;

namespace trape.cli.trader.Account
{
    public interface IAccountant : IDisposable
    {
        Task Start();

        void Stop();

        BinanceBalance GetBalance(string symbol);
    }
}
