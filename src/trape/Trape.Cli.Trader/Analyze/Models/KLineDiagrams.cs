using Binance.Net.Interfaces;

namespace Trape.Cli.Trader.Analyze.Models
{
    public class KLineDiagrams
    {
        #region Properties

        /// <summary>
        /// One Minute Kline
        /// </summary>
        public IBinanceStreamKline? OneMinute { get; private set; }

        /// <summary>
        /// Three Minutes Kline
        /// </summary>
        public IBinanceStreamKline? ThreeMinutes { get; private set; }

        /// <summary>
        /// Five Minutes Kline
        /// </summary>
        public IBinanceStreamKline? FiveMinutes { get; private set; }

        /// <summary>
        /// Fifteen Minutes Kline
        /// </summary>
        public IBinanceStreamKline? FifteenMinutes { get; private set; }

        /// <summary>
        /// Thirty Minutes Kline
        /// </summary>
        public IBinanceStreamKline? ThirtyMinutes { get; private set; }

        /// <summary>
        /// One Hour Kline
        /// </summary>
        public IBinanceStreamKline? OneHour { get; private set; }

        /// <summary>
        /// Two Hours Kline
        /// </summary>
        public IBinanceStreamKline? TwoHours { get; private set; }

        /// <summary>
        /// Four Hours Kline
        /// </summary>
        public IBinanceStreamKline? FourHour { get; private set; }

        /// <summary>
        /// Six Hours Kline
        /// </summary>
        public IBinanceStreamKline? SixHours { get; private set; }

        /// <summary>
        /// Eight Hours Kline
        /// </summary>
        public IBinanceStreamKline? EightHours { get; private set; }

        /// <summary>
        /// Twelve Hours Kline
        /// </summary>
        public IBinanceStreamKline? TwelveHours { get; private set; }

        /// <summary>
        /// One Day Kline
        /// </summary>
        public IBinanceStreamKline? OneDay { get; private set; }

        /// <summary>
        /// Three Days Kline
        /// </summary>
        public IBinanceStreamKline? ThreeDays { get; private set; }

        /// <summary>
        /// One Week Kline
        /// </summary>
        public IBinanceStreamKline? OneWeek { get; private set; }

        /// <summary>
        /// One Month Kline
        /// </summary>
        public IBinanceStreamKline? OneMonth { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the relevant Kline data
        /// </summary>
        /// <param name="binanceStreamKlineData"></param>
        public void Update(IBinanceStreamKlineData binanceStreamKlineData)
        {
            switch (binanceStreamKlineData.Data.Interval)
            {
                case Binance.Net.Enums.KlineInterval.OneMinute:
                    OneMinute = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.ThreeMinutes:
                    ThreeMinutes = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.FiveMinutes:
                    FiveMinutes = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.FifteenMinutes:
                    FifteenMinutes = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.ThirtyMinutes:
                    ThirtyMinutes = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.OneHour:
                    OneHour = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.TwoHour:
                    TwoHours = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.FourHour:
                    FourHour = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.SixHour:
                    SixHours = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.EightHour:
                    EightHours = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.TwelveHour:
                    TwelveHours = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.OneDay:
                    OneDay = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.ThreeDay:
                    ThreeDays = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.OneWeek:
                    OneWeek = binanceStreamKlineData.Data;
                    break;
                case Binance.Net.Enums.KlineInterval.OneMonth:
                    OneMonth = binanceStreamKlineData.Data;
                    break;
            }
        }

        #endregion
    }
}
