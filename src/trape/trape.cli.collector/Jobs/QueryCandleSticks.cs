using trape.cli.collector.DataLayer;
using Binance.Net.Objects;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace trape.cli.collector.Jobs
{
    [Job(0, 5, 0)]
    public class QueryCandleSticks : AbstractJob
    {
        internal string Symbol { get; }


        public QueryCandleSticks(string symbol)
        {
            this.Symbol = symbol;
        }

        public async override Task Execute(CancellationToken cancellationToken)
        {           

            //var client = new BinanceClient(new ClientConfiguration()
            //{
            //    ApiKey = Program.Configuration.GetSection("binance:apikey").Value,
            //    SecretKey = Program.Configuration.GetSection("binance:secretkey").Value,
            //    Logger = logger
            //});

            //var request = new BinanceExchange.API.Models.Request.GetKlinesCandlesticksRequest()
            //{
            //    StartTime = DateTime.UtcNow.AddMinutes(-6),
            //    Interval = BinanceExchange.API.Enums.KlineInterval.OneMinute,
            //    Symbol = Symbol,
            //    EndTime = DateTime.UtcNow
            //};

            //var candleSticks = await client.GetKlinesCandlesticks(request).ConfigureAwait(false);

            //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - {this.GetType().Name}: Received {candleSticks.Count} values between {DateTime.UtcNow.AddMinutes(-5).ToString("HH:mm:ss")} and {DateTime.UtcNow.ToString("HH:mm:ss")}");

            //using (var dbContext = new CoinTradeContext(Program.Configuration.GetConnectionString("CoinTradeDB")))
            //{
            //    foreach (var cs in candleSticks)
            //    {
            //        await dbContext.InsertCandleStick(request, cs, cancellationToken).ConfigureAwait(false);
            //    }
            //}
        }
    }
}
