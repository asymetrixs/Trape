using System;

namespace trape.cli.trader.Analyze
{
    public interface IAnalyst : IDisposable
    {
        Recommendation GetRecommendation(string symbol);

        void Start();

        void Finish();
    }
}
