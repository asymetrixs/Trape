using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace binance.cli.DataLayer
{
    public interface IBinanceContext
    {
        DbSet<CandleStick> CandleSticks { get; }

        DbSet<SymbolPrice> SymbolPrices { get; }
    }
}
