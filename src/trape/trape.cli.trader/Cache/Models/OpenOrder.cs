namespace trape.cli.trader.Cache.Models
{
    /// <summary>
    /// Class for caching of open orders
    /// </summary>
    public class OpenOrder
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>OpenOrder</c> class.
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="estimatedQuoteOrderQuantity">Estimated quote order quantity</param>
        public OpenOrder(string guid, string symbol, decimal estimatedQuoteOrderQuantity)
        {
            this.GUID = guid;
            this.Symbol = symbol;
            this.EstimatedQuoteOrderQuantity = estimatedQuoteOrderQuantity;
        }

        #endregion

        #region Properties

        /// <summary>
        /// GUID
        /// </summary>
        public string GUID { get; }

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Estimated quote order quantity
        /// </summary>
        public decimal EstimatedQuoteOrderQuantity { get; }

        #endregion
    }
}
