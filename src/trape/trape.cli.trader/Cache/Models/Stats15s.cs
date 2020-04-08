namespace trape.cli.trader.Cache.Models
{
    /// <summary>
    /// Class for stats of based on 15 second refresh
    /// </summary>
    public sealed class Stats15s : ITrend
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Stats15s</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="dataBasis">Data Basis</param>
        /// <param name="slope45s">Slope 45 seconds</param>
        /// <param name="slope1m">Slope 1 minute</param>
        /// <param name="slope2m">Slope 2 minutes</param>
        /// <param name="slope3m">Slope 3 minutes</param>
        /// <param name="movav45s">Moving Average 45 seconds</param>
        /// <param name="movav1m">Moving Average 1 minute</param>
        /// <param name="movav2m">Moving Average 2 minutes</param>
        /// <param name="movav3m">Moving Average 3 mintues</param>
        public Stats15s(string symbol, int dataBasis, decimal slope45s, decimal slope1m, decimal slope2m, decimal slope3m,
            decimal movav45s, decimal movav1m, decimal movav2m, decimal movav3m)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope45s = slope45s;
            this.Slope1m = slope1m;
            this.Slope2m = slope2m;
            this.Slope3m = slope3m;
            this.MovingAverage45s = movav45s;
            this.MovingAverage1m = movav1m;
            this.MovingAverage2m = movav2m;
            this.MovingAverage3m = movav3m;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Data Basis
        /// </summary>
        public int DataBasis { get; private set; }

        /// <summary>
        /// Slope 45 seconds
        /// </summary>
        public decimal Slope45s { get; private set; }

        /// <summary>
        /// Slope 1 minute
        /// </summary>
        public decimal Slope1m { get; private set; }

        /// <summary>
        /// Slope 2 minutes
        /// </summary>
        public decimal Slope2m { get; private set; }

        /// <summary>
        /// Slope 3 minutes
        /// </summary>
        public decimal Slope3m { get; private set; }

        /// <summary>
        /// Moving Average 45 seconds
        /// </summary>
        public decimal MovingAverage45s { get; private set; }

        /// <summary>
        /// Moving Average 1 minute
        /// </summary>
        public decimal MovingAverage1m { get; private set; }

        /// <summary>
        /// Moving Average 2 minutes
        /// </summary>
        public decimal MovingAverage2m { get; private set; }

        /// <summary>
        /// Moving Average 3 minutes
        /// </summary>
        public decimal MovingAverage3m { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// If true, then the data basis is valid
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            // Roughly 3 * 60 (3 Minutes)
            return this.DataBasis > 170;
        }

        #endregion
    }
}
