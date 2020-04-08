namespace trape.cli.trader.Cache.Models
{
    /// <summary>
    /// Class for stats of based on 2 minute refresh
    /// </summary>
    public sealed class Stats2m : ITrend
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Stats2m</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="dataBasis">Data Basis</param>
        /// <param name="slope5m">Slope 5 mintues</param>
        /// <param name="slope7m">Slope 7 minutes</param>
        /// <param name="slope10m">Slope 10 minutes</param>
        /// <param name="slope15m">Slope 15 minutes</param>
        /// <param name="movav5m">Moving Average 5 minutes</param>
        /// <param name="movav7m">Moving Average 7 minutes</param>
        /// <param name="movav10m">Moving Average 10 minutes</param>
        /// <param name="movav15m">Moving Average 15 minutes</param>
        public Stats2m(string symbol, int dataBasis, decimal slope5m, decimal slope7m, decimal slope10m, decimal slope15m,
            decimal movav5m, decimal movav7m, decimal movav10m, decimal movav15m)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope5m = slope5m;
            this.Slope7m = slope7m;
            this.Slope10m = slope10m;
            this.Slope15m = slope15m;
            this.MovingAverage5m = movav5m;
            this.MovingAverage7m = movav7m;
            this.MovingAverage10m = movav10m;
            this.MovingAverage15m = movav15m;
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
        /// Slope 5 minutes
        /// </summary>
        public decimal Slope5m { get; private set; }

        /// <summary>
        /// Slope 7 minutes
        /// </summary>
        public decimal Slope7m { get; private set; }

        /// <summary>
        /// Slope 10 minutes
        /// </summary>
        public decimal Slope10m { get; private set; }

        /// <summary>
        /// Slope 15 minutes
        /// </summary>
        public decimal Slope15m { get; private set; }

        /// <summary>
        /// Moving Average 5 minutes
        /// </summary>
        public decimal MovingAverage5m { get; private set; }

        /// <summary>
        /// Moving average 7 minutes
        /// </summary>
        public decimal MovingAverage7m { get; private set; }

        /// <summary>
        /// Moving Average 10 minutes
        /// </summary>
        public decimal MovingAverage10m { get; private set; }

        /// <summary>
        /// Moving Average 15 minutes
        /// </summary>
        public decimal MovingAverage15m { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// If true, then the data basis is valid
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            // Roughly 15 * 60 (15 Minutes)
            return this.DataBasis > 858;
        }

        #endregion
    }
}
