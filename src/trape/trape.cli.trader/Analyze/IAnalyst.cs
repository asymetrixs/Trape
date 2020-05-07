using System;
using trape.datalayer.Models;

namespace trape.cli.trader.Analyze
{
    public interface IAnalyst : IDisposable
    {
        /// <summary>
        /// Returns the latest recommendation for a symbol
        /// </summary>
        /// <param name="symbol">The symbol to get the recommendation for</param>
        /// <returns>Recommendation for a symbol</returns>
        Recommendation GetRecommendation(string symbol);

        /// <summary>
        /// Starts an analyst
        /// </summary>
        void Start();

        /// <summary>
        /// Stops an analyst
        /// </summary>
        void Finish();
    }
}
