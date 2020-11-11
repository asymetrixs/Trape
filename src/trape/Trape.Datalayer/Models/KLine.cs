using System;
using Trape.Datalayer.Enums;

namespace Trape.Datalayer.Models
{
    public class Kline
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Kline</c> class.
        /// </summary>
        public Kline()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Key
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The open price of this candlestick
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// The quote volume
        /// </summary>
        public decimal QuoteVolume { get; set; }

        /// <summary>
        /// Boolean indicating whether this candlestick is closed
        /// </summary>
        public bool Final { get; set; }

        /// <summary>
        /// The amount of trades in this candlestick
        /// </summary>
        public int TradeCount { get; set; }

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
        public decimal TakerBuyQuoteVolume { get; set; }

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
        public decimal TakerBuyBaseVolume { get; set; }

        /// <summary>
        /// The volume traded during this candle stick
        /// </summary>
        public decimal BaseVolume { get; set; }

        #endregion
    }
}
