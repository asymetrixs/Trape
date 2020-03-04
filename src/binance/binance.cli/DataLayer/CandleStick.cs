using System;
using System.Collections.Generic;
using System.Text;

namespace binance.cli.DataLayer
{
    public class CandleStick
    {
        public decimal Close { get; set; }

        public DateTime CloseTime { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public int NumberOfTrades { get; set; }

        public decimal Open { get; set; }

        public DateTime OpenTime { get; set; }

        public decimal QuoteAssetVolume { get; set; }

        public decimal TakerBuyBaseAssetVolume { get; set; }

        public decimal TakerBuyQuoteAssetVolume { get; set; }

        public decimal Volume { get; set; }
    }
}
