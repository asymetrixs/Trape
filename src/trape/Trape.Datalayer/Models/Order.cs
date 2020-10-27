using System;
using System.Collections.Generic;

namespace trape.datalayer.Models
{
    public class Order : AbstractKey
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Order</c> class.
        /// </summary>
        public Order()
        {
            OrderUpdates = new List<OrderUpdate>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Id
        /// </summary>
        public long OrderId { get; set; }

        /// <summary>
        /// Client order id
        /// </summary>        
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Client order
        /// </summary>
        public virtual ClientOrder ClientOrder { get; set; }

        /// <summary>
        /// Order List Id
        /// </summary>
        public long OrderListId { get; set; }

        /// <summary>
        /// Created On
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Order List
        /// </summary>
        public virtual OrderList OrderList { get; set; }

        /// <summary>
        /// Order Updates
        /// </summary>
        public virtual List<OrderUpdate> OrderUpdates { get; set; }

        #endregion
    }
}
