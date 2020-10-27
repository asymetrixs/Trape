using System;

namespace trape.datalayer.Models
{
    /// <summary>
    /// This class represents the latest price for 24 hours
    /// </summary>
    public class CurrentPrice
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>CurrentPrice</c> class.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="eventTime">Event Time</param>
        /// <param name="lowPrice">Low Price</param>
        /// <param name="highPrice">High Price</param>
        /// <param name="openPrice">Open Price</param>
        /// <param name="currentDayClosePrice">Current day close price</param>
        /// <param name="priceChangePercentage">Price change percentage</param>
        /// <param name="priceChange">Price change</param>
        public CurrentPrice(string symbol, DateTimeOffset eventTime, decimal lowPrice, decimal highPrice, decimal openPrice,
            decimal currentDayClosePrice, decimal priceChangePercentage, decimal priceChange)
        {
            CurrentDayClosePrice = currentDayClosePrice;
            EventTime = eventTime;
            HighPrice = highPrice;
            LowPrice = lowPrice;
            OpenPrice = openPrice;
            PriceChange = priceChange;
            PriceChangePercentage = priceChangePercentage;
            Symbol = symbol;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Event Time
        /// </summary>
        public DateTimeOffset EventTime { get; private set; }

        /// <summary>
        /// Low Price in 24h
        /// </summary>
        public decimal LowPrice { get; private set; }

        /// <summary>
        /// High Price in 24h
        /// </summary>
        public decimal HighPrice { get; private set; }

        /// <summary>
        /// Open Price 24h ago
        /// </summary>
        public decimal OpenPrice { get; private set; }

        /// <summary>
        /// Current day close price aka price now
        /// </summary>
        public decimal CurrentDayClosePrice { get; private set; }

        /// <summary>
        /// Price change percentage
        /// </summary>
        public decimal PriceChangePercentage { get; private set; }

        /// <summary>
        /// Price change
        /// </summary>
        public decimal PriceChange { get; private set; }

        #endregion
    }
}