using System;

namespace Trape.Cli.Trader.Analyze.Models
{
    public class Recommendation
    {
        #region Properties

        /// <summary>
        /// Best Ask Price
        /// </summary>
        public decimal BestAskPrice { get; set; }

        /// <summary>
        /// Best Bid Price
        /// </summary>
        public decimal BestBidPrice { get; set; }

        /// <summary>
        /// Created on
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        public ActionType Action { get; set; }

        /// <summary>
        /// Current Price
        /// </summary>
        public decimal Price { get; set; }

        #endregion
    }
}
