using System;
using System.Collections.Generic;
using Trape.Datalayer.Enums;

namespace Trape.Datalayer.Models
{
    public class PlacedOrder
    {
        #region Contructor

        /// <summary>
        /// Initializes a new instance of the <c>PlacedOrder</c> class.
        /// </summary>
        public PlacedOrder()
        {
            Fills = new List<OrderTrade>();
        }

        #endregion

        #region

        /// <summary>
        /// Only present if a margin trade happened
        /// </summary>
        public string MarginBuyBorrowAsset { get; set; }

        /// <summary>
        /// Date of creation
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// Only present if a margin trade happened
        /// </summary>
        public decimal? MarginBuyBorrowAmount { get; set; }

        /// <summary>
        /// Stop price for the order
        /// </summary>
        public decimal? StopPrice { get; set; }

        /// <summary>
        /// The side of the order
        /// </summary>
        public OrderSide Side { get; set; }

        /// <summary>
        /// The type of the order
        /// </summary>
        public OrderType Type { get; set; }

        /// <summary>
        /// For what time the order lasts
        /// </summary>
        public TimeInForce TimeInForce { get; set; }

        /// <summary>
        /// The current status of the order
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// The original quote order quantity
        /// </summary>
        public decimal QuoteQuantity { get; set; }

        /// <summary>
        /// Cummulative amount
        /// </summary>
        public decimal QuoteQuantityFilled { get; set; }

        /// <summary>
        /// The quantity of the order that is executed
        /// </summary>
        public decimal QuantityFilled { get; set; }

        /// <summary>
        /// The original quantity of the order
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// The price of the order
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// The time the order was placed
        /// </summary>
        public DateTime TransactionTime { get; set; }

        /// <summary>
        /// Original order id
        /// </summary>
        public string OriginalClientOrderId { get; set; }

        /// <summary>
        /// The order id as assigned by the client
        /// </summary>
        public string ClientOrderId { get; set; }

        /// <summary>
        /// The order id as assigned by Binance
        /// </summary>
        public long OrderId { get; set; }

        /// <summary>
        /// The symbol the order is for
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Id of the order list this order belongs to
        /// </summary>
        public long? OrderListId { get; set; }

        /// <summary>
        /// Fills for the order
        /// </summary>
        public virtual List<OrderTrade> Fills { get; set; }

        #endregion
    }
}
