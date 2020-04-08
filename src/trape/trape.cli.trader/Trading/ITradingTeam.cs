using System;
using System.Threading.Tasks;

namespace trape.cli.trader.Trading
{
    /// <summary>
    /// Interface for the <c>TradingTeam</c>
    /// </summary>
    public interface ITradingTeam : IDisposable
    {
        /// <summary>
        /// Starts the <c>TradingTeam</c>
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the <c>TradingTeam</c>
        /// </summary>
        /// <returns></returns>
        Task Finish();
    }
}