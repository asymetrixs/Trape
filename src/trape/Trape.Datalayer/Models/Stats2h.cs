using System.ComponentModel.DataAnnotations.Schema;

namespace trape.datalayer.Models
{
    /// <summary>
    /// [Deprecated] Class for stats of based on 2 hours refresh
    /// </summary>
    [NotMapped]
    public sealed class Stats2h
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Stats2h</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        public Stats2h(string symbol = null)
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
        /// [Deprecated] Slope 6 hours
        /// </summary>
        [Column("r_slope_6h")]
        public decimal Slope6h { get; private set; }

        /// <summary>
        /// [Deprecated] Slope 12 hours
        /// </summary>
        [Column("r_slope_12h")]
        public decimal Slope12h { get; private set; }

        /// <summary>
        /// [Deprecated] Slope 18 hours
        /// </summary>
        [Column("r_slope_18h")]
        public decimal Slope18h { get; private set; }

        /// <summary>
        /// [Deprecated] Slope 1 day
        /// </summary>
        [Column("r_slope_1d")]
        public decimal Slope1d { get; private set; }

        /// <summary>
        /// [Deprecated] Moving Average 6 hours
        /// </summary>
        [Column("r_movav_6h")]
        public decimal MovingAverage6h { get; private set; }

        /// <summary>
        /// [Deprecated] Moving Average 12 hours
        /// </summary>
        [Column("r_movav_12h")]
        public decimal MovingAverage12h { get; private set; }

        /// <summary>
        /// [Deprecated] Moving Average 18 hours
        /// </summary>
        [Column("r_movav_18h")]
        public decimal MovingAverage18h { get; private set; }

        /// <summary>
        /// [Deprecated] Moving Average 1 day
        /// </summary>
        [Column("r_movav_1d")]
        public decimal MovingAverage1d { get; private set; }

        #endregion
    }
}
