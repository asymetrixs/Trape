using System;
using System.Collections.Generic;
using System.Text;

namespace binance.cli.DataLayer
{
    public class SymbolPrice
    {

        public decimal Price { get; set; }

        public string Symbol { get; set; }

        public DateTimeOffset Timestamp { get; set; } = DateTime.UtcNow;
    }
}
