namespace trape.datalayer.Models
{
    public class OrderTrade
    {
        /// <summary>
        /// The id of the trade
        /// </summary>
        public long TradeId { get; set; }

        /// <summary>
        /// Price of the trade
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Quantity of the trade
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Commission paid over this trade
        /// </summary>
        public decimal Commission { get; set; }

        /// <summary>
        /// The asset the commission is paid in
        /// </summary>
        public string CommissionAsset { get; set; }

        /// <summary>
        /// When side is buy, reflects how much of it was sold
        /// </summary>
        public decimal ConsumedQuantity { get; set; }

        public long PlacedOrderId { get; set; }

        public virtual PlacedOrder PlacedOrder { get; set; }
    }
}
