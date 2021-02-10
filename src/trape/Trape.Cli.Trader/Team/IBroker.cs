using System;

namespace Trape.Cli.Trader.Team
{
    /// <summary>
    /// Interface for the <c>Broker</c>
    /// </summary>
    public interface IBroker : IDisposable, IStartable
    {
        /// <summary>
        /// Links a broker to an analyst
        /// </summary>
        /// <param name="analyst"></param>
        void SubscribeTo(IAnalyst analyst);
    }
}
