namespace Trape.Cli.Trader.Market
{
    using Binance.Net.Objects.Spot.SpotData;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Trape.Cli.Trader.Cache.Models;

    /// <summary>
    /// Interface for <c>StockExchange</c>
    /// </summary>
    public interface IStockExchange
    {
        /// <summary>
        /// Informs about Orders
        /// </summary>
        IObservable<BinancePlacedOrder> NewOrder { get; }

        /// <summary>
        /// Place an order
        /// </summary>
        /// <param name="clientOrder">Client Order</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task PlaceOrder(ClientOrder clientOrder, CancellationToken cancellationToken = default);
    }
}
