using Binance.Net.Objects.Spot.SpotData;
using System;
using System.Threading.Tasks;
using Trape.Cli.Trader.Account.Models;

namespace Trape.Cli.Trader.Account
{
    public interface IAccountant : IDisposable
    {
        #region Methods

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
        /// <param name="asset"></param>
        /// <returns></returns>
        Task<BinanceBalance?> GetBalance(string asset);

        /// <summary>
        /// Returns trade summary
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        TradeSummary? GetTradeSummary(string symbol);

        #endregion
    }
}
