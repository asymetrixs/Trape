namespace Trape.Cli.Trader.Team.Models
{
    public class Recommendation
    {
        /// <summary>
        /// Best Ask Price
        /// </summary>
        public decimal BestAskPrice { get; set; }

        /// <summary>
        /// Best Bid Price
        /// </summary>
        public decimal BestBidPrice { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        public ActionType Action { get; set; }

        /// <summary>
        /// Current Price
        /// </summary>
        public decimal Price { get; set; }
    }
}
