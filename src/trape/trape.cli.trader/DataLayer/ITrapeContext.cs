using Binance.Net.Objects;
using Binance.Net.Objects.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;
using trape.cli.trader.DataLayer.Models;

namespace trape.cli.trader.DataLayer
{
    public interface ITrapeContext
    {
        Task InsertAsync(Analyze.Recommendation decision, Stats3s trend3Seconds, Stats15s trend15Seconds,
            Stats2m trend2Minutes, Stats10m trend10Minutes, Stats2h trend2Hours, CancellationToken cancellationToken);

        Task<IEnumerable<Stats3s>> Get3SecondsTrendAsync(CancellationToken cancellationToken);

        Task<IEnumerable<Stats15s>> Get15SecondsTrendAsync(CancellationToken cancellationToken);

        Task<IEnumerable<Stats2m>> Get2MinutesTrendAsync(CancellationToken cancellationToken);

        Task<IEnumerable<Stats10m>> Get10MinutesTrendAsync(CancellationToken cancellationToken);

        Task<IEnumerable<Stats2h>> Get2HoursTrendAsync(CancellationToken cancellationToken);

        Task<decimal> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken);

        Task<IEnumerable<CurrentPrice>> GetCurrentPriceAsync(CancellationToken cancellationToken);

        Task InsertAsync(IEnumerable<BinanceStreamBalance> binanceStreamBalances, CancellationToken cancellationToken);

        Task InsertAsync(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate, CancellationToken cancellationToken);

        Task InsertAsync(BinanceStreamOrderList binanceStreamOrderList, CancellationToken cancellationToken);

        Task InsertAsync(BinanceStreamOrderUpdate binanceStreamOrderUpdate, CancellationToken cancellationToken);

        Task InsertAsync(Order order, CancellationToken cancellationToken);

        Task InsertAsync(BinancePlacedOrder binancePlacedOrder, CancellationToken cancellationToken);

        Task<Order> GetLastOrderAsync(string symbol, CancellationToken cancellationToken);
    }
}
