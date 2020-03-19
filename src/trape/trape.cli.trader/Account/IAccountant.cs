using Binance.Net.Objects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace trape.cli.trader.Account
{
    public interface IAccountant
    {
        Task Start();

        void Stop();

        BinanceBalance GetBinanceBalance(string symbol);
    }
}
