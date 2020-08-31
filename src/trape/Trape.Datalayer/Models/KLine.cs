using System;
using trape.datalayer.Enums;

namespace trape.datalayer.Models
{
    public class Kline : AbstractKey
    {
        /// <summary>
        /// The open price of this candlestick
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// The quote volume
        /// </summary>
        public decimal QuoteAssetVolume { get; set; }

        /// <summary>
        /// Boolean indicating whether this candlestick is closed
        /// </summary>
        public bool Final { get; set; }

        /// <summary>
        /// The amount of trades in this candlestick
        /// </summary>
        public int TradeCount { get; set; }

        /// <summary>
        /// The volume traded during this candlestick
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// The lowest price of this candlestick
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// The highest price of this candlestick
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// The close price of this candlestick
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// The quote volume of active buy
        /// </summary>
        public decimal TakerBuyQuoteAssetVolume { get; set; }

        /// <summary>
        /// The last trade id in this candlestick
        /// </summary>
        public long LastTradeId { get; set; }

        /// <summary>
        /// The first trade id in this candlestick
        /// </summary>
        public long FirstTradeId { get; set; }

        /// <summary>
        /// The interval of this candlestick
        /// </summary>
        public KlineInterval Interval { get; set; }

        /// <summary>
        /// The symbol this candlestick is for
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// The close time of this candlestick
        /// </summary>
        public DateTime CloseTime { get; set; }

        /// <summary>
        /// The open time of this candlestick
        /// </summary>
        public DateTime OpenTime { get; set; }

        /// <summary>
        /// The volume of active buy
        /// </summary>
        public decimal TakerBuyBaseAssetVolume { get; set; }
    }
}
