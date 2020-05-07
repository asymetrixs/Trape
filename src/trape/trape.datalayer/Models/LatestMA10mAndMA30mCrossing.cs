using System;

namespace trape.datalayer.Models
{
    /// <summary>
    /// Has information when slope10m and slope30m last crossed
    /// </summary>
    public class LatestMA10mAndMA30mCrossing
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
        public decimal Slope10m { get; }

        /// <summary>
        /// Slope 30m
        /// </summary>
        public decimal Slope30m { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>LatestMA10mAndMA30mCrossing</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="eventTime">Event time</param>
        /// <param name="slope10m">Slope 10m</param>
        /// <param name="slope30m">Slope 30m</param>
        public LatestMA10mAndMA30mCrossing(string symbol, DateTime eventTime, decimal slope10m, decimal slope30m)
        {
            this.Symbol = symbol;
            this.EventTime = eventTime;
            this.Slope10m = slope10m;
            this.Slope30m = slope30m;
        }

        #endregion
    }
}
