using System.ComponentModel.DataAnnotations.Schema;

namespace Trape.Datalayer.Models
{
    /// <summary>
    /// Class for stats of based on 10 minute refresh
    /// </summary>
    [Table("stats10m", Schema = "stubs")]
    public sealed class Stats10m
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Stats10m</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        public Stats10m(string symbol = null)
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
        /// Slope 30 minutes
        /// </summary>
        [Column("r_slope_30m")]
        public decimal Slope30m { get; private set; }

        /// <summary>
        /// Slope 1 hour
        /// </summary>
        [Column("r_slope_1h")]
        public decimal Slope1h { get; private set; }

        /// <summary>
        /// Slope 2 hours
        /// </summary>
        [Column("r_slope_2h")]
        public decimal Slope2h { get; private set; }

        /// <summary>
        /// Slope 3 hours
        /// </summary>
        [Column("r_slope_3h")]
        public decimal Slope3h { get; private set; }

        /// <summary>
        /// Moving Average 30 minutes
        /// </summary>
        [Column("r_movav_30m")]
        public decimal MovingAverage30m { get; private set; }

        /// <summary>
        /// Moving Average 1 hour
        /// </summary>
        [Column("r_movav_1h")]
        public decimal MovingAverage1h { get; private set; }

        /// <summary>
        /// Moving Average 2 hours
        /// </summary>
        [Column("r_movav_2h")]
        public decimal MovingAverage2h { get; private set; }

        /// <summary>
        /// Moving Average 3 hours
        /// </summary>
        [Column("r_movav_3h")]
        public decimal MovingAverage3h { get; private set; }

        #endregion
    }
}
