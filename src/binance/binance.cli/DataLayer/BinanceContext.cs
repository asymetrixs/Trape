using Microsoft.EntityFrameworkCore;


namespace binance.cli.DataLayer
{
    class BinanceContext : IBinanceContext
    {
        public BinanceContext()
            : base()
        {
        }

        public DbSet<CandleStick> CandleSticks { get; private set; }

        public DbSet<SymbolPrice> SymbolPrices { get; private set; }
    }
}
