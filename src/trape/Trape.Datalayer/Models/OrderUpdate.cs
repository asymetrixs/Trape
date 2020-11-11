using System;
using Trape.Datalayer.Enums;

namespace Trape.Datalayer.Models
{
    public class OrderUpdate : AbstractKey
    {
        #region Properties

        /// <summary>
        /// Last quote asset transacted quantity (i.e. LastPrice * LastQuantity)
        /// </summary>
        public decimal LastQuoteQuantity { get; set; }

        /// <summary>
        /// Time the order was created
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// The price of the last filled trade
        /// </summary>
        public decimal LastPriceFilled { get; set; }

        /// <summary>
        /// Quote order quantity
        /// </summary>
        public decimal QuoteOrderQuantity { get; set; }

        /// <summary>
        /// Whether the buyer is the maker
        /// </summary>
        public bool BuyerIsMaker { get; set; }

        /// <summary>
        /// Is working
        /// </summary>
        public bool IsWorking { get; set; }

        /// <summary>
        /// The trade id
        /// </summary>
        public long TradeId { get; set; }

        /// <summary>
        /// The asset the commission was taken from
        /// </summary>
        public string CommissionAsset { get; set; }

        /// <summary>
        /// The commission payed
        /// </summary>
        public decimal Commission { get; set; }

        /// <summary>
        /// The quantity of the last filled trade of this order
        /// </summary>
        public decimal LastQuantityFilled { get; set; }

        /// <summary>
        /// The quantity of all trades that were filled for that order
        /// </summary>
        public decimal QuantityFilled { get; set; }

        /// <summary>
        /// The id of the order as assigned by Binance
        /// </summary>
        public long OrderId { get; set; }

        /// <summary>
        /// Quote order quantity
        /// </summary>
        public decimal QuoteQuantity { get; set; }

        /// <summary>
        /// Time of the update
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// Cummulative amount
        /// </summary>
        public decimal QuoteQuantityFilled { get; set; }

        /// <summary>
        /// Order
        /// </summary>
        public virtual Order Order { get; set; }

        /// <summary>
        /// The reason the order was rejected
        /// </summary>
        public OrderRejectReason RejectReason { get; set; }

        /// <summary>
        /// The status of the order
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// The execution type
        /// </summary>
        public ExecutionType ExecutionType { get; set; }

        /// <summary>
        /// The original client order id
        /// </summary>
        public string OriginalClientOrderId { get; set; }

        /// <summary>
        /// The iceberg quantity of the order
        /// </summary>
        public decimal IcebergQuantity { get; set; }

        /// <summary>
        /// The stop price of the order
        /// </summary>
        public decimal StopPrice { get; set; }

        /// <summary>
        /// The price of the order
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// The quantity of the order
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// The timespan the order is active
        /// </summary>
        public TimeInForce TimeInForce { get; set; }

        /// <summary>
        /// The type of the order
        /// </summary>
        public OrderType Type { get; set; }

        /// <summary>
        /// The side of the order
        /// </summary>
        public OrderSide Side { get; set; }

        /// <summary>
        /// The new client order id
        /// </summary>
        public string ClientOrderId { get; set; }

        /// <summary>
        /// The symbol the order is for
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// This id of the corresponding order list. (-1 if not part of an order list)
        /// </summary>
        public long? OrderListId { get; set; }

        /// <summary>
        /// Order List
        /// </summary>
        public virtual OrderList OrderList { get; set; }

        /// <summary>
        /// [Unknown] Unused
        /// </summary>
        public long I { get; set; }

        #endregion
    }
}
