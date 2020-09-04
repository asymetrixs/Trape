using System;

namespace trape.datalayer.Models
{
    public class BookPrice
    {
        #region Properties

        /// <summary>
        /// Update id
        /// </summary>
        public long UpdateId { get; set; }

        /// <summary>
        /// The symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Price of the best bid
        /// </summary>
        public decimal BestBidPrice { get; set; }

        /// <summary>
        /// Quantity of the best bid
        /// </summary>
        public decimal BestBidQuantity { get; set; }

        /// <summary>
        /// Price of the best ask
        /// </summary>
        public decimal BestAskPrice { get; set; }

        /// <summary>
        /// Quantity of the best ask
        /// </summary>
        public decimal BestAskQuantity { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime TransactionTime { get; set; }

        #endregion
    }
}
