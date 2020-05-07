using System;
using System.Collections.Generic;

namespace trape.datalayer.Models
{
    public class Order : AbstractKey
    {
        /// <summary>
        /// The symbol of the order
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// The id of the order
        /// </summary>
        public long OrderId { get; set; }

        /// <summary>
        /// The client order id
        /// </summary>        
        public string ClientOrderId { get; set; }

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
    }
}
