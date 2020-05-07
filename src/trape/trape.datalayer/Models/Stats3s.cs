namespace trape.datalayer.Models
{
    /// <summary>
    /// Class for stats of based on 3 seconds refresh
    /// </summary>
    public sealed class Stats3s
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Stats3s</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="dataBasis">Data Basis</param>
        /// <param name="slope5s">Slope 5 seconds</param>
        /// <param name="slope10s">Slope 10 seconds</param>
        /// <param name="slope15s">Slope 15 seconds</param>
        /// <param name="slope30s">Slope 30 seconds</param>
        /// <param name="movav5s">Moving Average 5 seconds</param>
        /// <param name="movav10s">Moving Average 10 seconds</param>
        /// <param name="movav15s">Moving Average 15 seconds</param>
        /// <param name="movav30s">Moving Average 30 seconds</param>
        public Stats3s(string symbol, int dataBasis, decimal slope5s, decimal slope10s, decimal slope15s, decimal slope30s,
            decimal movav5s, decimal movav10s, decimal movav15s, decimal movav30s)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope5s = slope5s;
            this.Slope10s = slope10s;
            this.Slope15s = slope15s;
            this.Slope30s = slope30s;
            this.MovingAverage5s = movav5s;
            this.MovingAverage10s = movav10s;
            this.MovingAverage15s = movav15s;
            this.MovingAverage30s = movav30s;
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
        /// Slope 5 seconds
        /// </summary>
        public decimal Slope5s { get; private set; }

        /// <summary>
        /// Slope 10 seconds
        /// </summary>
        public decimal Slope10s { get; private set; }

        /// <summary>
        /// Slope 15 seconds
        /// </summary>
        public decimal Slope15s { get; private set; }

        /// <summary>
        /// Slope 30 seconds
        /// </summary>
        public decimal Slope30s { get; private set; }

        /// <summary>
        /// Moving Average 5 seconds
        /// </summary>
        public decimal MovingAverage5s { get; private set; }

        /// <summary>
        /// Moving Average 10 seconds
        /// </summary>
        public decimal MovingAverage10s { get; private set; }

        /// <summary>
        /// Moving Average 15 seconds
        /// </summary>
        public decimal MovingAverage15s { get; private set; }

        /// <summary>
        /// Moving Average 30 seconds
        /// </summary>
        public decimal MovingAverage30s { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// If true, then the data basis is valid
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            // Should have at least a value per second
            return this.DataBasis > 25;
        }

        #endregion
    }
}
