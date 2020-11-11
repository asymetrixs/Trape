using System;
using System.Threading.Tasks;

namespace Trape.Cli.trader.Team
{
    /// <summary>
    /// Interface for start/stopable classes.
    /// </summary>
    public interface IStartable : IDisposable
    {
        /// <summary>
        /// Symbol
        /// </summary>
        string Symbol { get; }

        /// <summary>
        /// Starts an instance
        /// </summary>
        /// <param name="symbol">Symbol</param>
        void Start(string symbol);

        /// <summary>
        /// Stops an instance
        /// </summary>
        Task Terminate();

        /// <summary>
        /// Last time item was active
        /// </summary>
        public DateTime LastActive { get; }
    }
}
