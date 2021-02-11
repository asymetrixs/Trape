namespace Trape.Cli.Trader.Cache.Models
{
    using Binance.Net.Enums;
    using System;

    /// <summary>
    /// Represents an order
    /// </summary>
    public class ClientOrder
    {
        /// <summary>
        /// Initializes a new instance of the <c>Order</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        public ClientOrder(string symbol)
        {
            this.Id = Guid.NewGuid().ToString("N");
            this.Symbol = symbol;
        }

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Event Time
        /// </summary>
        public DateTime CreatedOn { get; set; }

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
        /// Order response type
        /// </summary>
        public OrderResponseType OrderResponseType { get; set; }

        /// <summary>
        /// [IGNORED] Time in force
        /// </summary>
        public TimeInForce? TimeInForce { get; set; }
    }
}
