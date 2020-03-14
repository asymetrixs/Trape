using System;

namespace trape.cli.trader.Cache.Models
{
    public class CurrentPrice
    {
        public CurrentPrice(string symbol, DateTimeOffset eventTime, decimal lowPrice, decimal highPrice, decimal openPrice,
            decimal currentDayClosePrice, decimal priceChangePercentage, decimal priceChange)
        {
            this.CurrentDayClosePrice = currentDayClosePrice;
            this.EventTime = eventTime;
            this.HighPrice = highPrice;
            this.LowPrice = lowPrice;
            this.OpenPrice = openPrice;
            this.PriceChange = priceChange;
            this.PriceChangePercentage = priceChangePercentage;
            this.Symbol = symbol;
        }

        public string Symbol { get; private set; }

        public DateTimeOffset EventTime { get; private set; }

        public decimal LowPrice { get; private set; }

        public decimal HighPrice { get; private set; }

        public decimal OpenPrice { get; private set; }

        public decimal CurrentDayClosePrice { get; private set; }

        public decimal PriceChangePercentage { get; private set; }

        public decimal PriceChange { get; private set; }
    }
}