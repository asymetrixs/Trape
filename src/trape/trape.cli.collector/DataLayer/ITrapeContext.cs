using Binance.Net.Objects;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace trape.cli.collector.DataLayer
{
    public interface ITrapeContext : IDisposable
    {
        Task Insert(BinanceStreamTick binanceStreamTick, CancellationToken cancellationToken);

        Task Insert(BinanceStreamKlineData binanceStreamKlineData, CancellationToken cancellationToken);

        Task Insert(BinanceBookTick binanceBookTick, CancellationToken cancellationToken);

        Task<int> CleanUpBookTicks(CancellationToken cancellationToken);
    }
}
