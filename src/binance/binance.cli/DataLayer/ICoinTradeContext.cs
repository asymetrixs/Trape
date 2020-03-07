using Binance.Net.Objects;
using System.Threading;
using System.Threading.Tasks;

namespace binance.cli.DataLayer
{
    public interface ICoinTradeContext
    {

        Task Insert(BinanceStreamTick binanceStreamTick, CancellationToken cancellationToken);

        Task Insert(BinanceStreamKlineData binanceStreamKlineData, CancellationToken cancellationToken);
    }
}
