using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace trape.datalayer.Models
{
    /// <summary>
    /// Has information when slope10m and slope30m last crossed
    /// </summary>
    [Table("latest_ma1h_and_ma3h_crossing", Schema = "stubs")]
    public class LatestMA1hAndMA3hCrossing
    {
        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        [Column("symbol")]
        public string Symbol { get; private set; }

        /// <summary>
        /// Time of crossing
        /// </summary>
        [Column("event_time")]
        public DateTime EventTime { get; private set; }

        /// <summary>
        /// Slope 1h
        /// </summary>
        [Column("slope1h")]
        public decimal Slope1h { get; private set; }

        /// <summary>
        /// Slope 3h
        /// </summary>
        [Column("slope3h")]
        public decimal Slope3h { get; private set; }

        #endregion
    }
}
