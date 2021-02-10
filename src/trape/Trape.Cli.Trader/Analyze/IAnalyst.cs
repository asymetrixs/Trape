using System;
using Trape.Cli.trader.Team;
using Trape.Cli.Trader.Analyze.Models;

namespace Trape.Cli.trader.Analyze
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
