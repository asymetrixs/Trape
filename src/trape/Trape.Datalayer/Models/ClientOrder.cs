﻿using System;
using Trape.Datalayer.Enums;

namespace Trape.Datalayer.Models
{
    /// <summary>
    /// Represents an order
    /// </summary>
    public class ClientOrder
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Order</c> class.
        /// </summary>
        public ClientOrder()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Initializes a new instance of the <c>Order</c> class.
        /// </summary>
        /// <param name="id">Id of the client order</param>
        public ClientOrder(string id)
        {
            Id = id;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Event Time
        /// </summary>
        public DateTime CreatedOn { get; set; }

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
        public decimal Quantity { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// New client order id
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Order
        /// </summary>
        public virtual Order Order { get; set; }

        /// <summary>
        /// Order Id
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// Order response type
        /// </summary>
        public OrderResponseType OrderResponseType { get; set; }

        /// <summary>
        /// [IGNORED] Time in force
        /// </summary>
        public TimeInForce? TimeInForce { get; set; }

        #endregion
    }
}
