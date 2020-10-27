using System.ComponentModel.DataAnnotations.Schema;

namespace trape.datalayer.Models
{
    /// <summary>
    /// Class for stats of based on 2 minute refresh
    /// </summary>
    [Table("stats2m", Schema = "stubs")]
    public sealed class Stats2m
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Stats2m</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        public Stats2m(string symbol = null)
        {
            Symbol = symbol;
        }

        #endregion

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
        /// Slope 5 minutes
        /// </summary>
        [Column("r_slope_5m")]
        public decimal Slope5m { get; private set; }

        /// <summary>
        /// Slope 7 minutes
        /// </summary>
        [Column("r_slope_7m")]
        public decimal Slope7m { get; private set; }

        /// <summary>
        /// Slope 10 minutes
        /// </summary>
        [Column("r_slope_10m")]
        public decimal Slope10m { get; private set; }

        /// <summary>
        /// Slope 15 minutes
        /// </summary>
        [Column("r_slope_15m")]
        public decimal Slope15m { get; private set; }

        /// <summary>
        /// Moving Average 5 minutes
        /// </summary>
        [Column("r_movav_5m")]
        public decimal MovingAverage5m { get; private set; }

        /// <summary>
        /// Moving average 7 minutes
        /// </summary>
        [Column("r_movav_7m")]
        public decimal MovingAverage7m { get; private set; }

        /// <summary>
        /// Moving Average 10 minutes
        /// </summary>
        [Column("r_movav_10m")]
        public decimal MovingAverage10m { get; private set; }

        /// <summary>
        /// Moving Average 15 minutes
        /// </summary>
        [Column("r_movav_15m")]
        public decimal MovingAverage15m { get; private set; }

        #endregion
    }
}
