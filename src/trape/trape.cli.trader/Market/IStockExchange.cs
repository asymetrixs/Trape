using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.DataLayer.Models;

namespace trape.cli.trader.Market
{
    /// <summary>
    /// Interface for <c>StockExchange</c>
    /// </summary>
    public interface IStockExchange
    {
        Task PlaceOrder(Order order, CancellationToken cancellationToken = default);
    }
}
