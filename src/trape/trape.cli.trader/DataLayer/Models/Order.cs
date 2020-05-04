using Binance.Net.Objects;
using System;

namespace trape.cli.trader.DataLayer.Models
{
    /// <summary>
    /// Represents an order
    /// </summary>
    public class Order
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Order</c> class.
        /// </summary>
        public Order()
        {
            this.NewClientOrderId = Guid.NewGuid().ToString("N");
        }

        #endregion

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
        public string NewClientOrderId { get; }

        /// <summary>
        /// Order response type
        /// </summary>
        public OrderResponseType OrderResponseType { get; set; }

        /// <summary>
        /// [IGNORED] Time in force
        /// </summary>
        public TimeInForce TimeInForce { get; set; }
             
        #endregion
    }
}
