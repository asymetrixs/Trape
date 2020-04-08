using Binance.Net.Objects;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace trape.cli.collector.DataLayer
{
    /// <summary>
    /// Interface describing the TrapeContext
    /// </summary>
    public interface ITrapeContext : IDisposable
    {
        /// <summary>
        /// Inserts <c>BinanceStreamTick</c> instances into the database
        /// </summary>
        /// <param name="binanceStreamTick"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Insert(BinanceStreamTick binanceStreamTick, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts <c>BinanceStreamKlineData</c> instances into the database
        /// </summary>
        /// <param name="binanceStreamKlineData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Insert(BinanceStreamKlineData binanceStreamKlineData, CancellationToken cancellationToken);

        /// <summary>
        /// Inserts <c>BinanceBookTick</c> instances into the database
        /// </summary>
        /// <param name="binanceBookTick"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Insert(BinanceBookTick binanceBookTick, CancellationToken cancellationToken);

        /// <summary>
        /// Cleans up the <c>BinanceBookTick</c>s
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> CleanUpBookTicks(CancellationToken cancellationToken);
    }
}
