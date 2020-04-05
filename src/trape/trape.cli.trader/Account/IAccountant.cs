using Binance.Net.Objects;
using System;
using System.Threading.Tasks;

namespace trape.cli.trader.Account
{
    public interface IAccountant : IDisposable
    {
        #region Methods

        Task Start();

        Task Finish();

        Task<BinanceBalance> GetBalance(string symbol);

        #endregion
    }
}
