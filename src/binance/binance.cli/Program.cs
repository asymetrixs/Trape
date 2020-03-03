using BinanceExchange.API.Client;
using log4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace binance.cli
{
    class Program
    {
        public static IConfigurationRoot Configuration;

        async static Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var exampleProgramLogger = LogManager.GetLogger(typeof(Program));
            exampleProgramLogger.Debug("Logging Test");

            //Initialise the general client client with config
            var client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = Configuration.GetSection("binance:apikey").Value,
                SecretKey = Configuration.GetSection("binance:secretkey").Value,
                Logger = exampleProgramLogger
            });

            // Test the Client
            //var response = await client.TestConnectivity();
            
            //var exchangeInfo = await client.GetExchangeInfo();

            //var systemStatus = await client.GetSystemStatus();

            //var symbolsPriceTicker = await client.GetSymbolsPriceTicker();

            var btcPrice = await client.GetPrice("BTCUSDT");
            var candleStick = await client.GetKlinesCandlesticks(new BinanceExchange.API.Models.Request.GetKlinesCandlesticksRequest()
            {
                StartTime = DateTime.UtcNow.AddDays(-1),
                Interval = BinanceExchange.API.Enums.KlineInterval.OneMinute,
                Symbol = "BTCUSDT",
                EndTime = DateTime.UtcNow
            });

            Console.WriteLine("Hello World!");
        }
    }
}
