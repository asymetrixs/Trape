using Binance.Net.Objects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.collector.DataLayer.Models;

namespace trape.cli.collector.DataLayer
{
    /// <summary>
    /// Interface describing the TrapeContext
    /// </summary>
    public interface ITrapeContext : IDisposable
    {
        DbSet<Symbol> Symbols { get; }

        /// <summary>
        /// Inserts <c>BinanceStreamTick</c> instances into the database
        /// </summary>
        /// <param name="binanceStreamTick"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Insert(BinanceStreamTick binanceStreamTick, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts <c>BinanceStreamKlineData</c> instances into the database
        /// </summary>
        /// <param name="binanceStreamKlineData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Insert(BinanceStreamKlineData binanceStreamKlineData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts <c>BinanceBookTick</c> instances into the database
        /// </summary>
        /// <param name="binanceBookTick"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Insert(BinanceBookTick binanceBookTick, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up the <c>BinanceBookTick</c>s
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> CleanUpBookTicks(CancellationToken cancellationToken = default);
    }
}
