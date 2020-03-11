using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Trends;

namespace trape.cli.trader.DataLayer
{
    public interface ITrapeContext
    {
        Task Insert(string symbol, string decision, Trend3Seconds trend3Seconds, Trend15Seconds trend15Seconds,
            Trend2Minutes trend2Minutes, Trend10Minutes trend10Minutes, Trend2Hours trend2Hours, CancellationToken cancellationToken);

        Task<IEnumerable<Trend3Seconds>> Get3SecondsTrend(CancellationToken cancellationToken);

        Task<IEnumerable<Trend15Seconds>> Get15SecondsTrend(CancellationToken cancellationToken);

        Task<IEnumerable<Trend2Minutes>> Get2MinutesTrend(CancellationToken cancellationToken);

        Task<IEnumerable<Trend10Minutes>> Get10MinutesTrend(CancellationToken cancellationToken);

        Task<IEnumerable<Trend2Hours>> Get2HoursTrend(CancellationToken cancellationToken);

        Task<decimal> GetCurrentPrice(string symbol, CancellationToken cancellationToken);
    }
}
