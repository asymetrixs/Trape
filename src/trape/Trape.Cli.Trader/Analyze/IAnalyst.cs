using System;
using Trape.Cli.trader.Team;
using Trape.Datalayer.Models;

namespace Trape.Cli.trader.Analyze
{
    /// <summary>
    /// Interface for the <c>Analyst</c>
    /// </summary>
    public interface IAnalyst : IDisposable, IStartable
    {
        IObservable<Recommendation> NewRecommendation { get; }
    }
}
