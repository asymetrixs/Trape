namespace Trape.Cli.Trader.Account.Models
{
    public class TradeSummary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TradeSummary"/> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="quoteQuantity">Quote quantity</param>
        public TradeSummary(string symbol, decimal quantity, decimal quoteQuantity)
        {
            this.Symbol = symbol;
            this.Quantity = quantity;
            this.QuoteQuantity = quoteQuantity;
        }

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Asset Quantity
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal QuoteQuantity { get; }

        /// <summary>
        /// Price per symbol unit
        /// </summary>
        public decimal PricePerUnit => 1 / this.Quantity * this.QuoteQuantity;
    }
}
