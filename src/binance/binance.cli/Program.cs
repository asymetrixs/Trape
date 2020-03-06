using binance.cli.DataLayer;
using binance.cli.Jobs;
using BinanceExchange.API.Client;
using log4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace binance.cli
{
    class Program
    {
        public static IConfigurationRoot Configuration;
                
        static void Main(string[] args)
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

            //var exchangeInfo = await client.GetExchangeInfo().ConfigureAwait(false);

            //var systemStatus = await client.GetSystemStatus();

            //var symbolsPriceTicker = await client.GetSymbolsPriceTicker().ConfigureAwait(false);

            //var btcPrice = await client.GetPrice("BTCUSDT").ConfigureAwait(false);

            Console.WriteLine("JobManager started");
            JobManager.Start();

            
            Console.ReadLine();

            JobManager.Stop();
            Console.WriteLine("JobManager stopped");
            JobManager.Terminate();
            Console.WriteLine("JobManager terminated");
        }
    }
}
