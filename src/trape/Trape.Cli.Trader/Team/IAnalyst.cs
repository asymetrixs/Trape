namespace Trape.Cli.Trader.Team
{
    using System;
    using Trape.Cli.Trader.Team.Models;

    /// <summary>
    /// Interface for the <c>Analyst</c>
    /// </summary>
    public interface IAnalyst : IDisposable, IStartable
    {
        /// <summary>
        /// Holds recommendations
        /// </summary>
        IObservable<Recommendation> NewRecommendation { get; }
    }
}
