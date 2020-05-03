using Binance.Net.Objects;
using System.Threading;
using System.Threading.Tasks;

namespace trape.cli.trader.Market
{
    /// <summary>
    /// Interface for <c>StockExchange</c>
    /// </summary>
    public interface IStockExchange
    {
        Task PlaceOrder(string symbol, OrderSide orderSide, OrderType orderType, decimal quoteOrderQuantity, decimal price,
            CancellationToken cancellationToken);
    }
}
