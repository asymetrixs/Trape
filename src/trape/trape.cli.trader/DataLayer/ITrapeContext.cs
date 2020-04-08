using Binance.Net.Objects;
using Binance.Net.Objects.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;
using trape.cli.trader.DataLayer.Models;

namespace trape.cli.trader.DataLayer
{
    /// <summary>
    /// Interface for TrapeContext
    /// </summary>
    public interface ITrapeContext
    {
        /// <summary>
        /// Stores a recommendation with stats in the database
        /// </summary>
        /// <param name="recommendation">Recommendation</param>
        /// <param name="trend3Seconds">Trend 3 seconds</param>
        /// <param name="trend15Seconds">Trend 15 seconds</param>
        /// <param name="trend2Minutes">Trend 2 minutes</param>
        /// <param name="trend10Minutes">Trend 10 minutes</param>
        /// <param name="trend2Hours">Trend 2 hours</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task InsertAsync(Analyze.Recommendation recommendation, Stats3s trend3Seconds, Stats15s trend15Seconds,
            Stats2m trend2Minutes, Stats10m trend10Minutes, Stats2h trend2Hours, CancellationToken cancellationToken);

        /// <summary>
        /// Returns stats based on 3 seconds
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<IEnumerable<Stats3s>> Get3SecondsTrendAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns stats based on 15 seconds
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<IEnumerable<Stats15s>> Get15SecondsTrendAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns stats based on 2 minutes
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<IEnumerable<Stats2m>> Get2MinutesTrendAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns stats based on 10 minutes
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<IEnumerable<Stats10m>> Get10MinutesTrendAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns stats based on 2 hours
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<IEnumerable<Stats2h>> Get2HoursTrendAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns a list latest current prices for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<decimal> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a list of current prices, one for each symbol
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<IEnumerable<CurrentPrice>> GetCurrentPriceAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stores <c>BinanceStreamBalance</c> in the database
        /// </summary>
        /// <param name="binanceStreamBalances">Binance Stream Balances</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task InsertAsync(IEnumerable<BinanceStreamBalance> binanceStreamBalances, CancellationToken cancellationToken);

        /// <summary>
        /// Stores <c>BinanceStreamBalanceUpdate</c> in the database
        /// </summary>
        /// <param name="binanceStreamBalanceUpdate">Binance Stream Balances</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task InsertAsync(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Stores <c>BinanceStreamOrderList</c> in the database
        /// </summary>
        /// <param name="binanceStreamOrderList">Binance Stream Balances</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task InsertAsync(BinanceStreamOrderList binanceStreamOrderList, CancellationToken cancellationToken);

        /// <summary>
        /// Stores <c>BinanceStreamOrderUpdate</c> in the database
        /// </summary>
        /// <param name="binanceStreamOrderUpdate">Binance Stream Balances</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task InsertAsync(BinanceStreamOrderUpdate binanceStreamOrderUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Stores <c>Order</c> in the database
        /// </summary>
        /// <param name="order">Binance Stream Balances</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task InsertAsync(Order order, CancellationToken cancellationToken);

        /// <summary>
        /// Stores <c>BinancePlacedOrder</c> in the database
        /// </summary>
        /// <param name="binancePlacedOrder">Binance Stream Balances</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task InsertAsync(BinancePlacedOrder binancePlacedOrder, CancellationToken cancellationToken);

        /// <summary>
        /// Queries the database for a list of last orders
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        Task<IEnumerable<LastOrder>> GetLastOrdersAsync(string symbol, CancellationToken cancellationToken);
    }
}
