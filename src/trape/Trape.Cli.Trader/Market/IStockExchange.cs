using Binance.Net.Objects.Spot.SpotData;
using System;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.Trader.Cache.Models;

namespace Trape.Cli.trader.Market
{
    /// <summary>
    /// Interface for <c>StockExchange</c>
    /// </summary>
    public interface IStockExchange
    {
        /// <summary>
        /// Place an order
        /// </summary>
        /// <param name="order"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task PlaceOrder(ClientOrder clientOrder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Informs about Orders
        /// </summary>
        IObservable<BinancePlacedOrder> NewOrder { get; }
    }
}
