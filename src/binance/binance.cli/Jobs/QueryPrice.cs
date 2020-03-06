using binance.cli.DataLayer;
using BinanceExchange.API.Client;
using log4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace binance.cli.Jobs
{
    [Job(0, 0, 1)]
    public class QueryPrice : AbstractJob
    {
        internal string Symbol { get; }

        public QueryPrice(string symbol)
        {
            this.Symbol = symbol;
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
                        
            var btcPrice = await client.GetPrice(this.Symbol).ConfigureAwait(false);

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - {this.GetType().Name}: {btcPrice.Symbol} is {btcPrice.Price}");

            using (var dbContext = new CoinTradeContext(Program.Configuration.GetConnectionString("CoinTradeDB")))
            {                
                await dbContext.InsertPrice(DateTime.UtcNow, btcPrice, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
