using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace binance.cli.DataLayer.Models
{
    public class CandleStick
    {
        [PgName("close")]
        public decimal Close { get; set; }

        [PgName("close_time")]
        public DateTime CloseTime { get; set; }

        [PgName("high")]
        public decimal High { get; set; }

        [PgName("low")]
        public decimal Low { get; set; }

        [PgName("number_of_trades")]
        public int NumberOfTrades { get; set; }

        [PgName("open")]
        public decimal Open { get; set; }

        [PgName("open_time")]
        public DateTime OpenTime { get; set; }

        [PgName("quote_asset_volume")]
        public decimal QuoteAssetVolume { get; set; }

        [PgName("taker_buy_base_asset_volume")]
        public decimal TakerBuyBaseAssetVolume { get; set; }

        [PgName("taker_buy_quote_asset_volume")]
        public decimal TakerBuyQuoteAssetVolume { get; set; }

        [PgName("volume")]
        public decimal Volume { get; set; }
    }
}
