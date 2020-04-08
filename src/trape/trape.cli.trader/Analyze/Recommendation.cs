using System;

namespace trape.cli.trader.Analyze
{
    /// <summary>
    /// This class represents a recommendation given by an <c>Analyst</c> to an <c>Trader</c>
    /// </summary>
    public class Recommendation
    {
        /// <summary>
        /// Symbol the recommendation is for
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// The current price the recommendation is valid for
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// The action recommended by the <c>Analyst</c> instance
        /// </summary>
        public Action Action { get; set; }

        /// <summary>
        /// Date and Time when the recommendation was created
        /// </summary>
        public DateTimeOffset EventTime { get; set; }
    }
}
