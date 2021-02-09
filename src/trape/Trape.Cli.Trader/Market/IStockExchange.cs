using Binance.Net.Objects.Spot.SpotData;
using System;
using System.Threading;
using System.Threading.Tasks;
using Trape.Datalayer.Models;

namespace Trape.Cli.trader.Market
{
    /// <summary>
    /// Interface for <c>StockExchange</c>
    /// </summary>
    public interface IStockExchange
    {
        Task PlaceOrder(ClientOrder order, CancellationToken cancellationToken = default);


        /// <summary>
        /// Informs about Orders
        /// </summary>
        IObservable<BinancePlacedOrder> NewOrder { get; }
    }
}
