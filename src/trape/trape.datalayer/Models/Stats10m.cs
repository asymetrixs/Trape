namespace trape.datalayer.Models
{
    /// <summary>
    /// Class for stats of based on 10 minute refresh
    /// </summary>
    public sealed class Stats10m
    {
        #region Contructor

        /// <summary>
        /// Initializes a new instance of the <c>Stats10m</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="dataBasis">Data Basis</param>
        /// <param name="slope30m">Slope 30 minutes</param>
        /// <param name="slope1h">Slope 1 hour</param>
        /// <param name="slope2h">Slope 2 hours</param>
        /// <param name="slope3h">Slope 3 hours</param>
        /// <param name="movav30m">Moving Average 30 minutes</param>
        /// <param name="movav1h">Moving Average 1 hour</param>
        /// <param name="movav2h">Moving Average 2 hours</param>
        /// <param name="movav3h">Moving Average 3 hours</param>
        public Stats10m(string symbol, int dataBasis, decimal slope30m, decimal slope1h, decimal slope2h, decimal slope3h,
            decimal movav30m, decimal movav1h, decimal movav2h, decimal movav3h)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope30m = slope30m;
            this.Slope1h = slope1h;
            this.Slope2h = slope2h;
            this.Slope3h = slope3h;
            this.MovingAverage30m = movav30m;
            this.MovingAverage1h = movav1h;
            this.MovingAverage2h = movav2h;
            this.MovingAverage3h = movav3h;
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
        /// Slope 30 minutes
        /// </summary>
        public decimal Slope30m { get; private set; }

        /// <summary>
        /// Slope 1 hour
        /// </summary>
        public decimal Slope1h { get; private set; }

        /// <summary>
        /// Slope 2 hours
        /// </summary>
        public decimal Slope2h { get; private set; }

        /// <summary>
        /// Slope 3 hours
        /// </summary>
        public decimal Slope3h { get; private set; }

        /// <summary>
        /// Moving Average 30 minutes
        /// </summary>
        public decimal MovingAverage30m { get; private set; }

        /// <summary>
        /// Moving Average 1 hour
        /// </summary>
        public decimal MovingAverage1h { get; private set; }

        /// <summary>
        /// Moving Average 2 hours
        /// </summary>
        public decimal MovingAverage2h { get; private set; }

        /// <summary>
        /// Moving Average 3 hours
        /// </summary>
        public decimal MovingAverage3h { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// If true, then the data basis is valid
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            // Roughly 3 * 60 * 60 (3 hours)
            return this.DataBasis > 10300;
        }

        #endregion
    }
}
