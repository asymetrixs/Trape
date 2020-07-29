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
        /// The close time of these stats
        /// </summary>
        public DateTime CloseTime { get; set; }

        /// <summary>
        /// The open time of these stats
        /// </summary>
        public DateTime OpenTime { get; set; }

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
        public decimal AskQuantity { get; set; }

        /// <summary>
        /// The best ask price in the order book
        /// </summary>
        public decimal AskPrice { get; set; }

        /// <summary>
        /// The quantity of the best bid price available
        /// </summary>
        public decimal BidQuantity { get; set; }

        /// <summary>
        /// The best bid price in the order book
        /// </summary>
        public decimal BidPrice { get; set; }

        /// <summary>
        /// The most recent trade quantity
        /// </summary>
        public decimal LastQuantity { get; set; }

        /// <summary>
        /// The current day close price. This is the latest price for this symbol.
        /// </summary>
        public decimal LastPrice { get; set; }

        /// <summary>
        /// The close price of the previous day
        /// </summary>
        public decimal PrevDayClosePrice { get; set; }

        /// <summary>
        /// The weighted average
        /// </summary>
        public decimal WeightedAveragePrice { get; set; }

        /// <summary>
        /// The price change percentage of this symbol
        /// </summary>
        public decimal PriceChangePercent { get; set; }

        /// <summary>
        /// The price change of this symbol
        /// </summary>
        public decimal PriceChange { get; set; }

        /// <summary>
        /// The symbol this data is for
        /// </summary>
        public string Symbol { get; set; }
    }
}
