namespace Trape.Cli.Trader.Team
{
    using System;

    /// <summary>
    /// Interface for the <c>Broker</c>
    /// </summary>
    public interface IBroker : IDisposable, IStartable
    {
        /// <summary>
        /// Links a broker to an analyst
        /// </summary>
        /// <param name="analyst">Analyst</param>
        void SubscribeTo(IAnalyst analyst);
    }
}
