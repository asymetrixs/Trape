using NpgsqlTypes;
using System;

namespace trape.cli.collector.DataLayer.Models
{
    public class SymbolPrice
    {
        [PgName("remote_time")]
        public DateTimeOffset RemoteTime { get; set; } = DateTime.UtcNow;

        [PgName("symbol")]
        public string Symbol { get; set; }

        [PgName("price")]
        public decimal Price { get; set; }

    }
}
