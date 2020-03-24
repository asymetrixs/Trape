using Binance.Net.Objects;
using System;

namespace trape.cli.trader.DataLayer.Models
{
    public class Order
    {
        public long Id { get; set; }

        public DateTimeOffset EventTime { get; set; }

        public string Symbol { get; set; }

        public OrderSide Side { get; set; }

        public OrderType Type { get; set; }

        public decimal QuoteOrderQuantity { get; set; }

        public decimal Price { get; set; }

        public string NewClientOrderId { get; set; }

        public OrderResponseType OrderResponseType { get; set; }

        public TimeInForce TimeInForce { get; set; }

    }
}
