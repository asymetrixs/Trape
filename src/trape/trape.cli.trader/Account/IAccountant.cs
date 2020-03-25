using Binance.Net.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace trape.cli.trader.Account
{
    public interface IAccountant : IDisposable
    {
        Task Start();

        Task Stop();

        Task<BinanceBalance> GetBalance(string symbol);

        IEnumerable<decimal> GetAvailablePricesAndQuantities(string symbol);
    }
}
