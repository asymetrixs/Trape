namespace trape.datalayer.Models
{
    /// <summary>
    /// [Deprecated] Class for stats of based on 2 hours refresh
    /// </summary>
    public sealed class Stats2h
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Stats2h</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="dataBasis">Data Basis</param>
        /// <param name="slope6h">Slope 6 hours</param>
        /// <param name="slope12h">Slope 12 hours</param>
        /// <param name="slope18h">Slope 18 hours</param>
        /// <param name="slope1d">Slope 1 day</param>
        /// <param name="movav6h">Moving Average 6 hours</param>
        /// <param name="movav12h">Moving Average 12 hours</param>
        /// <param name="movav18h">Moving Average 18 hours</param>
        /// <param name="movav1d">Moving Average 1 day</param>
        public Stats2h(string symbol, int dataBasis, decimal slope6h, decimal slope12h, decimal slope18h, decimal slope1d,
            decimal movav6h, decimal movav12h, decimal movav18h, decimal movav1d)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope6h = slope6h;
            this.Slope12h = slope12h;
            this.Slope18h = slope18h;
            this.Slope1d = slope1d;
            this.MovingAverage6h = movav6h;
            this.MovingAverage12h = movav12h;
            this.MovingAverage18h = movav18h;
            this.MovingAverage1d = movav1d;
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
        /// [Deprecated] Slope 6 hours
        /// </summary>
        public decimal Slope6h { get; private set; }

        /// <summary>
        /// [Deprecated] Slope 12 hours
        /// </summary>
        public decimal Slope12h { get; private set; }

        /// <summary>
        /// [Deprecated] Slope 18 hours
        /// </summary>
        public decimal Slope18h { get; private set; }

        /// <summary>
        /// [Deprecated] Slope 1 day
        /// </summary>
        public decimal Slope1d { get; private set; }

        /// <summary>
        /// [Deprecated] Moving Average 6 hours
        /// </summary>
        public decimal MovingAverage6h { get; private set; }

        /// <summary>
        /// [Deprecated] Moving Average 12 hours
        /// </summary>
        public decimal MovingAverage12h { get; private set; }

        /// <summary>
        /// [Deprecated] Moving Average 18 hours
        /// </summary>
        public decimal MovingAverage18h { get; private set; }

        /// <summary>
        /// [Deprecated] Moving Average 1 day
        /// </summary>
        public decimal MovingAverage1d { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// [Deprecated] If true, then the data basis is valid
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            // Roughly 24 * 60 * 60 (24 hours)
            return this.DataBasis > 86200;
        }

        #endregion
    }
}
