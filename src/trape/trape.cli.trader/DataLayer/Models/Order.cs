using Binance.Net.Objects;
using System;

namespace trape.cli.trader.DataLayer.Models
{
    /// <summary>
    /// Represents an order
    /// </summary>
    public class Order
    {
        #region Properties

        /// <summary>
        /// Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Event Time
        /// </summary>
        public DateTimeOffset EventTime { get; set; }

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Side
        /// </summary>
        public OrderSide Side { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public OrderType Type { get; set; }

        /// <summary>
        /// Quote order quantity
        /// </summary>
        public decimal QuoteOrderQuantity { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// New client order id
        /// </summary>
        public string NewClientOrderId { get; set; }

        /// <summary>
        /// Order response type
        /// </summary>
        public OrderResponseType OrderResponseType { get; set; }

        /// <summary>
        /// Time in force
        /// </summary>
        public TimeInForce TimeInForce { get; set; }

        #endregion
    }
}
