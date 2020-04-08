using Binance.Net.Objects;
using System;

namespace trape.cli.trader.DataLayer.Models
{
    /// <summary>
    /// Represents a closed order
    /// </summary>
    public class LastOrder
    {
        #region Properties

        /// <summary>
        /// Binance Placed Order Id
        /// </summary>
        public long BinancePlacedOrderId { get; set; }

        /// <summary>
        /// Transaction Time
        /// </summary>
        public DateTimeOffset TransactionTime { get; set; }

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Side
        /// </summary>
        public OrderSide Side { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Consumed
        /// </summary>
        public decimal Consumed { get; set; }

        /// <summary>
        /// Consumed Price
        /// </summary>
        public decimal ConsumedPrice { get; set; }

        #endregion
    }
}
