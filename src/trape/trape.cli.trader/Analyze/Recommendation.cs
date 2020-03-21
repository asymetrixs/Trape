using System;

namespace trape.cli.trader.Analyze
{
    public class Recommendation
    {
        public string Symbol { get; set; }

        public decimal Price { get; set; }

        public Action Action { get; set; }

        public DateTimeOffset EventTime { get; set; }
    }
}
