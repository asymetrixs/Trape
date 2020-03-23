using System;

namespace trape.cli.trader.Analyze
{
    public interface IRecommender : IDisposable
    {
        Recommendation GetRecommendation(string symbol);

        void Start();

        void Stop();
    }
}
