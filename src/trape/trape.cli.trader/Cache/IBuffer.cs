using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;

namespace trape.cli.trader.Cache
{
    public interface IBuffer : IDisposable
    {
        IEnumerable<Trend3Seconds> Trends3Seconds { get; }

        IEnumerable<Trend15Seconds> Trends15Seconds { get; }

        IEnumerable<Trend2Minutes> Trends2Minutes { get; }

        IEnumerable<Trend10Minutes> Trends10Minutes { get; }

        IEnumerable<Trend2Hours> Trends2Hours { get; }

        IEnumerable<CurrentPrice> CurrentPrices { get; }

        Task Start();

        void Stop();

        IEnumerable<string> GetSymbols();
    }
}
