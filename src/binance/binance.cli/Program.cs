using binance.cli.Jobs;
using log4net;
using log4net.Core;
using Microsoft.Extensions.Configuration;
using System;
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
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

            var logger = LogManager.GetLogger(typeof(Program));
            
            logger.Debug("Logging Test");
            logger.Error("Logging Test");
            logger.Warn("Logging Test");
            logger.Info("Logging Test");

            //Initialise the general client client with config
            //var client = new BinanceClient(new ClientConfiguration()
            //{
            //    ApiKey = Configuration.GetSection("binance:apikey").Value,
            //    SecretKey = Configuration.GetSection("binance:secretkey").Value,
            //    Logger = exampleProgramLogger
            //});

            // Test the Client
            //var response = await client.TestConnectivity();

            //var exchangeInfo = await client.GetExchangeInfo().ConfigureAwait(false);

            //var systemStatus = await client.GetSystemStatus();

            //var symbolsPriceTicker = await client.GetSymbolsPriceTicker().ConfigureAwait(false);

            //var btcPrice = await client.GetPrice("BTCUSDT").ConfigureAwait(false);

            Console.WriteLine("JobManager started");
            //JobManager.Start();

            
            Console.ReadLine();

            JobManager.Stop();
            Console.WriteLine("JobManager stopped");
            JobManager.Terminate();
            Console.WriteLine("JobManager terminated");
        }

        private static void Configure()
        {
            
        }
    }
}
