using Binance.Net.Objects;
using Binance.Net.Objects.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;

namespace trape.cli.trader.DataLayer
{
    public interface ITrapeContext
    {
        Task Insert(Analyze.Recommendation decision, Stats3s trend3Seconds, Stats15s trend15Seconds,
            Stats2m trend2Minutes, Stats10m trend10Minutes, Stats2h trend2Hours, CancellationToken cancellationToken);

        Task<IEnumerable<Stats3s>> Get3SecondsTrend(CancellationToken cancellationToken);

        Task<IEnumerable<Stats15s>> Get15SecondsTrend(CancellationToken cancellationToken);

        Task<IEnumerable<Stats2m>> Get2MinutesTrend(CancellationToken cancellationToken);

        Task<IEnumerable<Stats10m>> Get10MinutesTrend(CancellationToken cancellationToken);

        Task<IEnumerable<Stats2h>> Get2HoursTrend(CancellationToken cancellationToken);

        Task<decimal> GetCurrentPrice(string symbol, CancellationToken cancellationToken);

        Task<IEnumerable<CurrentPrice>> GetCurrentPrice(CancellationToken cancellationToken);

        Task Insert(IEnumerable<BinanceStreamBalance> binanceStreamBalances, CancellationToken cancellationToken);

        Task Insert(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate, CancellationToken cancellationToken);

        Task Insert(BinanceStreamOrderList binanceStreamOrderList, CancellationToken cancellationToken);

        Task Insert(BinanceStreamOrderUpdate binanceStreamOrderUpdate, CancellationToken cancellationToken);
    }
}
