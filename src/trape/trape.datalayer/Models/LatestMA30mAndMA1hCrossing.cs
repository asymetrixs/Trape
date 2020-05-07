using System;

namespace trape.datalayer.Models
{
    /// <summary>
    /// Has information when slope10m and slope30m last crossed
    /// </summary>
    public class LatestMA30mAndMA1hCrossing
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
        public decimal Slope30m { get; }

        /// <summary>
        /// Slope 30m
        /// </summary>
        public decimal Slope1h { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>LatestMA10mAndMA30mCrossing</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="eventTime">Event time</param>
        /// <param name="slope30m">Slope 10m</param>
        /// <param name="slope1h">Slope 30m</param>
        public LatestMA30mAndMA1hCrossing(string symbol, DateTime eventTime, decimal slope30m, decimal slope1h)
        {
            this.Symbol = symbol;
            this.EventTime = eventTime;
            this.Slope30m = slope30m;
            this.Slope1h = slope1h;
        }

        #endregion
    }
}
