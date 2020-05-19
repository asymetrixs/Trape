using System;
using System.Threading.Tasks;

namespace trape.cli.trader.Trading
{
    /// <summary>
    /// Interface for the <c>Broker</c>
    /// </summary>
    public interface IBroker : IDisposable
    {
        /// <summary>
        /// Symbol
        /// </summary>
        string Symbol { get; }

        /// <summary>
        /// Starts the <c>Broker</c>
        /// </summary>
        /// <param name="symbolToTrade"></param>
        void Start(string symbolToTrade);

        /// <summary>
        /// Stops the <c>Broker</c>
        /// </summary>
        /// <returns></returns>
        Task Terminate();
    }
}
