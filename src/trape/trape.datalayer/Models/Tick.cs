using System;

namespace trape.datalayer.Models
{
    public class Tick : AbstractKey
    {
        /// <summary>
        /// The total trades of id
        /// </summary>
        public long TotalTrades { get; set; }

        /// <summary>
        /// The last trade id of today
        /// </summary>
        public long LastTradeId { get; set; }

        /// <summary>
        /// The first trade id of today
        /// </summary>
        public long FirstTradeId { get; set; }

        /// <summary>
        /// Total traded volume in the quote asset
        /// </summary>
        public decimal TotalTradedQuoteAssetVolume { get; set; }

        /// <summary>
        /// Total traded volume in the base asset
        /// </summary>
        public decimal TotalTradedBaseAssetVolume { get; set; }

        /// <summary>
        /// Todays low price
        /// </summary>
        public decimal LowPrice { get; set; }

        /// <summary>
        /// Todays high price
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// Todays open price
        /// </summary>
        public decimal OpenPrice { get; set; }

        /// <summary>
        /// The quantity of the best ask price
        /// </summary>
        public decimal BestAskQuantity { get; set; }

        /// <summary>
        /// The best ask price in the order book
        /// </summary>
        public decimal BestAskPrice { get; set; }

        /// <summary>
        /// The quantity of the best bid price available
        /// </summary>
        public decimal BestBidQuantity { get; set; }

        /// <summary>
        /// The best bid price in the order book
        /// </summary>
        public decimal BestBidPrice { get; set; }

        /// <summary>
        /// The current day close quantity.
        /// </summary>
        public decimal CloseTradesQuantity { get; set; }

        /// <summary>
        /// The current day close price. This is the latest price for this symbol.
        /// </summary>
        public decimal CurrentDayClosePrice { get; set; }

        /// <summary>
        /// The close price of the previous day
        /// </summary>
        public decimal PrevDayClosePrice { get; set; }

        /// <summary>
        /// The weighted average
        /// </summary>
        public decimal WeightedAverage { get; set; }

        /// <summary>
        /// The price change percentage of this symbol
        /// </summary>
        public decimal PriceChangePercentage { get; set; }

        /// <summary>
        /// The price change of this symbol
        /// </summary>
        public decimal PriceChange { get; set; }

        /// <summary>
        /// The symbol this data is for
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// The open time of these stats
        /// </summary>
        public DateTime StatisticsOpenTime { get; set; }

        /// <summary>
        /// The close time of these stats
        /// </summary>
        public DateTime StatisticsCloseTime { get; set; }
    }
}
