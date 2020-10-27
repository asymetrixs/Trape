using System.ComponentModel.DataAnnotations.Schema;

namespace trape.datalayer.Models
{
    /// <summary>
    /// Class for stats of based on 15 second refresh
    /// </summary>
    [NotMapped]
    public sealed class Stats15s
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Stats15s</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        public Stats15s(string symbol = null)
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
        /// Slope 45 seconds
        /// </summary>
        [Column("r_slope_45s")]
        public decimal Slope45s { get; private set; }

        /// <summary>
        /// Slope 1 minute
        /// </summary>
        [Column("r_slope_1m")]
        public decimal Slope1m { get; private set; }

        /// <summary>
        /// Slope 2 minutes
        /// </summary>
        [Column("r_slope_2m")]
        public decimal Slope2m { get; private set; }

        /// <summary>
        /// Slope 3 minutes
        /// </summary>
        [Column("r_slope_3m")]
        public decimal Slope3m { get; private set; }

        /// <summary>
        /// Moving Average 45 seconds
        /// </summary>
        [Column("r_movav_45s")]
        public decimal MovingAverage45s { get; private set; }

        /// <summary>
        /// Moving Average 1 minute
        /// </summary>
        [Column("r_movav_1m")]
        public decimal MovingAverage1m { get; private set; }

        /// <summary>
        /// Moving Average 2 minutes
        /// </summary>
        [Column("r_movav_2m")]
        public decimal MovingAverage2m { get; private set; }

        /// <summary>
        /// Moving Average 3 minutes
        /// </summary>
        [Column("r_movav_3m")]
        public decimal MovingAverage3m { get; private set; }

        #endregion
    }
}
