using System;

namespace Trape.Cli.trader.Cache.Models
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
        /// <param name="id">GUID</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="estimatedQuoteOrderQuantity">Estimated quote order quantity</param>
        public OpenOrder(string id, string symbol, decimal estimatedQuoteOrderQuantity)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Quantity = estimatedQuoteOrderQuantity;
            CreatedOn = DateTime.UtcNow;
        }

        #endregion

        #region Properties

        /// <summary>
        /// GUID
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Estimated quote order quantity
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Created on
        /// </summary>
        public DateTime CreatedOn { get; }

        #endregion
    }
}
