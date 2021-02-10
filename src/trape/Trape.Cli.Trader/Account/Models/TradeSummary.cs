namespace Trape.Cli.Trader.Account.Models
{
    public class TradeSummary
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; init; }

        /// <summary>
        /// Asset Quantity
        /// </summary>
        public decimal Quantity { get; init; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal QuoteQuantity { get; init; }

        /// <summary>
        /// Price per symbol unit
        /// </summary>
        public decimal PricePerUnit => 1 / Quantity * QuoteQuantity;
    }
}
