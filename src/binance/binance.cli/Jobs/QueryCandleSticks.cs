using BinanceExchange.API.Client;
using log4net;
using System;
using System.Threading.Tasks;

namespace binance.cli.Jobs
{
    [Job(0, 1, 0)]
    public class QueryCandleSticks : AbstractJob
    {
        public string Symbol { get; }

        public QueryCandleSticks(string symbol)
        {
            this.Symbol = symbol;
        }

        public async override Task Execute()
        {
            var logger = LogManager.GetLogger(typeof(Program));
            logger.Debug("Logging Test");

            var client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = Program.Configuration.GetSection("binance:apikey").Value,
                SecretKey = Program.Configuration.GetSection("binance:secretkey").Value,
                Logger = logger
            });

            var candleStick = await client.GetKlinesCandlesticks(new BinanceExchange.API.Models.Request.GetKlinesCandlesticksRequest()
            {
                StartTime = DateTime.UtcNow.AddMinutes(3),
                Interval = BinanceExchange.API.Enums.KlineInterval.OneMinute,
                Symbol = Symbol,
                EndTime = DateTime.UtcNow
            }).ConfigureAwait(false);


        }
    }
}
