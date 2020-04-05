using Binance.Net.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;

namespace trape.cli.trader.Cache
{
    public interface IBuffer : IDisposable
    {
        IEnumerable<Stats3s> Stats3s { get; }

        IEnumerable<Stats15s> Stats15s { get; }

        IEnumerable<Stats2m> Stats2m { get; }

        IEnumerable<Stats10m> Stats10m { get; }

        IEnumerable<Stats2h> Stats2h { get; }

        IEnumerable<CurrentPrice> CurrentPrices { get; }

        Task Start();

        void Finish();

        IEnumerable<string> GetSymbols();

        decimal GetAskPrice(string symbol);

        decimal GetBidPrice(string symbol);

        BinanceSymbol GetExchangeInfoFor(string symbol);
    }
}
