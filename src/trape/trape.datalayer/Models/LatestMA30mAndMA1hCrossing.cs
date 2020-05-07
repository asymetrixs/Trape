using System;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Column("symbol")]
        public string Symbol { get; }

        /// <summary>
        /// Time of crossing
        /// </summary>
        [Column("event_time")]
        public DateTime EventTime { get; }

        /// <summary>
        /// Slope 10m
        /// </summary>
        [Column("slope30m")]
        public decimal Slope30m { get; }

        /// <summary>
        /// Slope 30m
        /// </summary>
        [Column("slope1h")]
        public decimal Slope1h { get; }

        #endregion
    }
}
