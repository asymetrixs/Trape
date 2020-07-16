using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace trape.datalayer.Models
{
    /// <summary>
    /// Has information when slope10m and slope30m last crossed
    /// </summary>
    [Table("latest_ma10m_and_ma30m_crossing", Schema = "stubs")]
    public class LatestMA10mAndMA30mCrossing
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
        [Column("slope10m")]
        public decimal Slope10m { get; }

        /// <summary>
        /// Slope 30m
        /// </summary>
        [Column("slope30m")]
        public decimal Slope30m { get; }

        #endregion
    }
}
