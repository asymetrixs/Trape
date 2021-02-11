namespace Trape.Cli.Trader.Account
{
    using Binance.Net.Objects.Spot.SpotData;
    using System;
    using System.Threading.Tasks;
    using Trape.Cli.Trader.Account.Models;

    public interface IAccountant : IDisposable
    {
        /// <summary>
        /// Starts
        /// </summary>
        /// <returns></returns>
        Task Start();

        /// <summary>
        /// Terminates
        /// </summary>
        /// <returns></returns>
        Task Terminate();

        /// <summary>
        /// Returns balance
        /// </summary>
        /// <param name="asset">Asset</param>
        /// <returns></returns>
        Task<BinanceBalance?> GetBalance(string asset);

        /// <summary>
        /// Returns trade summary
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        TradeSummary? GetTradeSummary(string symbol);
    }
}
