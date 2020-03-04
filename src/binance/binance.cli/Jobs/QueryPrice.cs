using BinanceExchange.API.Client;
using log4net;
using System;
using System.Threading.Tasks;

namespace binance.cli.Jobs
{
    [Job(0, 0, 5)]
    public class QueryPrice : AbstractJob
    {
        public string Symbol { get; }

        public QueryPrice(string symbol)
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

            var btcPrice = await client.GetPrice("BTCUSDT").ConfigureAwait(false);
            


        }
    }
}
