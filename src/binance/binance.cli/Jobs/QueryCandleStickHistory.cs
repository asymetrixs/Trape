using binance.cli.DataLayer;
using BinanceExchange.API.Client;
using log4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace binance.cli.Jobs
{
    //[Job(0, 10, 0)]
    public class QueryCandleStickHistory : AbstractJob
    {
        internal string Symbol { get; }

        internal DateTimeOffset Start { get; private set; }

        private int _chunksInMinutes = 300;

        internal QueryCandleStickHistory(string symbol, DateTimeOffset start)
        {
            this.Symbol = symbol;
            this.Start = start;
        }

        public async override Task Execute(CancellationToken cancellationToken)
        {
            var logger = LogManager.GetLogger(typeof(Program));
            logger.Debug("Logging Test");

            var client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = Program.Configuration.GetSection("binance:apikey").Value,
                SecretKey = Program.Configuration.GetSection("binance:secretkey").Value,
                Logger = logger
            });

            var request = new BinanceExchange.API.Models.Request.GetKlinesCandlesticksRequest()
            {
                StartTime = this.Start.UtcDateTime.AddMinutes(-this._chunksInMinutes),
                Interval = BinanceExchange.API.Enums.KlineInterval.OneMinute,
                Symbol = Symbol,
                EndTime = this.Start.UtcDateTime
            };

            this.Start = request.StartTime.Value.AddMinutes(-this._chunksInMinutes);

            var candleSticks = await client.GetKlinesCandlesticks(request).ConfigureAwait(false);

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - {this.GetType().Name}: Received {candleSticks.Count} values between {this.Start.AddMinutes(-this._chunksInMinutes).ToString("HH:mm:ss")} and {this.Start.ToString("HH:mm:ss")}");

            using (var dbContext = new CoinTradeContext(Program.Configuration.GetConnectionString("CoinTradeDB")))
            {
                foreach (var cs in candleSticks)
                {
                    await dbContext.InsertCandleStick(request, cs, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
