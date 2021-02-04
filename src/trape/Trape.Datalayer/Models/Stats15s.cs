using System.ComponentModel.DataAnnotations.Schema;

namespace Trape.Datalayer.Models
{
    /// <summary>
    /// Class for stats of based on 15 second refresh
    /// </summary>
    [Table("stats15s", Schema = "stubs")]
    public sealed class Stats15s
    {
        #region Constructor

        public Stats15s() { }

        public Stats15s(decimal slope45s, decimal slope1m, decimal slope2m, decimal slope3m)
        {
            this.Slope45s = slope45s;
            this.Slope1m = slope1m;
            this.Slope2m = slope2m;
            this.Slope3m = slope3m;
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
