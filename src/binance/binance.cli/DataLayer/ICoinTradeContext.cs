using binance.cli.DataLayer.Models;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace binance.cli.DataLayer
{
    public interface ICoinTradeContext
    {
        DbSet<CandleStick> CandleSticks { get; }

        DbSet<SymbolPrice> SymbolPrices { get; }

        Task InsertCandleStick(GetKlinesCandlesticksRequest request, KlineCandleStickResponse result, CancellationToken cancellationToken);

        Task InsertPrice(DateTimeOffset datetime, SymbolPriceResponse price, CancellationToken cancellationToken);
    }
}
