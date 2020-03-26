using Binance.Net.Objects;
using System;

namespace trape.cli.trader.DataLayer.Models
{
    public class LastOrder
    {
        public long BinancePlacedOrderId { get; set; }

        public DateTimeOffset TransactionTime { get; set; }

        public string Symbol { get; set; }

        public OrderSide Side { get; set; }

        public decimal Price { get; set; }

        public decimal Quantity { get; set; }

        public decimal Consumed { get; set; }

        public decimal ConsumedPrice { get; set; }
    }
}
