using System;

namespace trape.cli.trader.Cache.Models
{
    /// <summary>
    /// Has information when slope10m and slope30m last crossed
    /// </summary>
    public class LatestMA1hAndMA3hCrossing
    {
        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Time of crossing
        /// </summary>
        public DateTime EventTime { get; }

        /// <summary>
        /// Slope 10m
        /// </summary>
        public decimal Slope1h { get; }

        /// <summary>
        /// Slope 30m
        /// </summary>
        public decimal Slope3h { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>LatestMA1hAnd3hCrossing</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="eventTime">Event time</param>
        /// <param name="slope1h">Slope 10m</param>
        /// <param name="slope3h">Slope 30m</param>
        public LatestMA1hAndMA3hCrossing(string symbol, DateTime eventTime, decimal slope1h, decimal slope3h)
        {
            this.Symbol = symbol;
            this.EventTime = eventTime;
            this.Slope1h = slope1h;
            this.Slope3h = slope3h;
        }

        #endregion
    }
}
