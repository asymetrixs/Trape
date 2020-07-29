using System;

namespace trape.datalayer.Models
{
    public class Recommendation : AbstractKey
    {
        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Created on
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        public Enums.Action Action { get; set; }

        /// <summary>
        /// Current Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Slope 5s
        /// </summary>
        public decimal Slope5s { get; set; }

        /// <summary>
        /// Moving Average 5s
        /// </summary>
        public decimal MovingAverage5s { get; set; }

        /// <summary>
        /// Slope 10s
        /// </summary>
        public decimal Slope10s { get; set; }

        /// <summary>
        /// Moving Average 10s
        /// </summary>
        public decimal MovingAverage10s { get; set; }

        /// <summary>
        /// Slope 15s
        /// </summary>
        public decimal Slope15s { get; set; }

        /// <summary>
        /// Moving Average 15s
        /// </summary>
        public decimal MovingAverage15s { get; set; }

        /// <summary>
        /// Slope 30s
        /// </summary>
        public decimal Slope30s { get; set; }

        /// <summary>
        /// Moving Average 30s
        /// </summary>
        public decimal MovingAverage30s { get; set; }

        /// <summary>
        /// Slope 45s
        /// </summary>
        public decimal Slope45s { get; set; }

        /// <summary>
        /// Moving Average 45s
        /// </summary>
        public decimal MovingAverage45s { get; set; }

        /// <summary>
        /// Slope 1m
        /// </summary>
        public decimal Slope1m { get; set; }

        /// <summary>
        /// Moving Average 1m
        /// </summary>
        public decimal MovingAverage1m { get; set; }

        /// <summary>
        /// Slope 2m
        /// </summary>
        public decimal Slope2m { get; set; }

        /// <summary>
        /// Moving Average 2m
        /// </summary>
        public decimal MovingAverage2m { get; set; }

        /// <summary>
        /// Slope 3m
        /// </summary>
        public decimal Slope3m { get; set; }

        /// <summary>
        /// Moving Average 3m
        /// </summary>
        public decimal MovingAverage3m { get; set; }

        /// <summary>
        /// Slope 5m
        /// </summary>
        public decimal Slope5m { get; set; }

        /// <summary>
        /// Moving Average 5m
        /// </summary>
        public decimal MovingAverage5m { get; set; }

        /// <summary>
        /// Slope 7m
        /// </summary>
        public decimal Slope7m { get; set; }

        /// <summary>
        /// Moving Average 7m
        /// </summary>
        public decimal MovingAverage7m { get; set; }

        /// <summary>
        /// Slope 10m
        /// </summary>
        public decimal Slope10m { get; set; }

        /// <summary>
        /// Moving Average 10m
        /// </summary>
        public decimal MovingAverage10m { get; set; }

        /// <summary>
        /// Slope 15m
        /// </summary>
        public decimal Slope15m { get; set; }

        /// <summary>
        /// Moving Average 15m
        /// </summary>
        public decimal MovingAverage15m { get; set; }

        /// <summary>
        /// Slope 30m
        /// </summary>
        public decimal Slope30m { get; set; }

        /// <summary>
        /// Moving Average 30m
        /// </summary>
        public decimal MovingAverage30m { get; set; }

        /// <summary>
        /// Slope 1h
        /// </summary>
        public decimal Slope1h { get; set; }

        /// <summary>
        /// Moving Average 1h
        /// </summary>
        public decimal MovingAverage1h { get; set; }

        /// <summary>
        /// Slope 2h
        /// </summary>
        public decimal Slope2h { get; set; }

        /// <summary>
        /// Moving Average 2h
        /// </summary>
        public decimal MovingAverage2h { get; set; }

        /// <summary>
        /// Slope 3h
        /// </summary>
        public decimal Slope3h { get; set; }

        /// <summary>
        /// Moving Average 3h
        /// </summary>
        public decimal MovingAverage3h { get; set; }

        /// <summary>
        /// Slope 6h
        /// </summary>
        public decimal Slope6h { get; set; }

        /// <summary>
        /// Moving Average 6h
        /// </summary>
        public decimal MovingAverage6h { get; set; }

        /// <summary>
        /// Slope 12h
        /// </summary>
        public decimal Slope12h { get; set; }

        /// <summary>
        /// Moving Average 12h
        /// </summary>
        public decimal MovingAverage12h { get; set; }

        /// <summary>
        /// Slope 18h
        /// </summary>
        public decimal Slope18h { get; set; }

        /// <summary>
        /// Moving Average 18h
        /// </summary>
        public decimal MovingAverage18h { get; set; }

        /// <summary>
        /// Slope 1d
        /// </summary>
        public decimal Slope1d { get; set; }

        /// <summary>
        /// Moving Average 1d
        /// </summary>
        public decimal MovingAverage1d { get; set; }

        #endregion
    }
}
