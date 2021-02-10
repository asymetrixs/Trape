using System;
using Trape.Cli.Trader.Team.Models;

namespace Trape.Cli.Trader.Team
{
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
