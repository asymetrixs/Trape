using System.ComponentModel.DataAnnotations.Schema;

namespace Trape.Datalayer.Models
{
    /// <summary>
    /// Class for stats of based on 3 seconds refresh
    /// </summary>
    [Table("stats3s", Schema = "stubs")]
    public sealed class Stats3s
    {
        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        [Column("r_symbol")]
        public string Symbol { get; private set; }

        /// <summary>
        /// Data Basis
        /// </summary>
        [Column("r_databasis")]
        public int DataBasis { get; private set; }

        /// <summary>
        /// Slope 5 seconds
        /// </summary>
        [Column("r_slope_5s")]
        public decimal Slope5s { get; private set; }

        /// <summary>
        /// Slope 10 seconds
        /// </summary>
        [Column("r_slope_10s")]
        public decimal Slope10s { get; private set; }

        /// <summary>
        /// Slope 15 seconds
        /// </summary>
        [Column("r_slope_15s")]
        public decimal Slope15s { get; private set; }

        /// <summary>
        /// Slope 30 seconds
        /// </summary>
        [Column("r_slope_30s")]
        public decimal Slope30s { get; private set; }

        /// <summary>
        /// Moving Average 5 seconds
        /// </summary>
        [Column("r_movav_5s")]
        public decimal MovingAverage5s { get; private set; }

        /// <summary>
        /// Moving Average 10 seconds
        /// </summary>
        [Column("r_movav_10s")]
        public decimal MovingAverage10s { get; private set; }

        /// <summary>
        /// Moving Average 15 seconds
        /// </summary>
        [Column("r_movav_15s")]
        public decimal MovingAverage15s { get; private set; }

        /// <summary>
        /// Moving Average 30 seconds
        /// </summary>
        [Column("r_movav_30s")]
        public decimal MovingAverage30s { get; private set; }

        #endregion
    }
}
