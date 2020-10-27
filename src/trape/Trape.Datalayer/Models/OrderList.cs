using System;
using System.Collections.Generic;
using trape.datalayer.Enums;

namespace trape.datalayer.Models
{
    public class OrderList : AbstractKey
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>OrderList</c> class.
        /// </summary>
        public OrderList()
        {
            Orders = new List<Order>();
            OrderUpdates = new List<OrderUpdate>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The id of the order list
        /// </summary>
        public long OrderListId { get; set; }

        /// <summary>
        /// The contingency type
        /// </summary>
        public string ContingencyType { get; set; }

        /// <summary>
        /// The order list status
        /// </summary>
        public ListStatusType ListStatusType { get; set; }

        /// <summary>
        /// The order status
        /// </summary>
        public ListOrderStatus ListOrderStatus { get; set; }

        /// <summary>
        /// The client id of the order list
        /// </summary>
        public string ListClientOrderId { get; set; }

        /// <summary>
        /// The transaction time
        /// </summary>
        public DateTime TransactionTime { get; set; }

        /// <summary>
        /// The symbol of the order list
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// The order in this list
        /// </summary>
        public virtual List<Order> Orders { get; set; }

        /// <summary>
        /// Order Updates
        /// </summary>
        public virtual List<OrderUpdate> OrderUpdates { get; set; }

        #endregion
    }
}
