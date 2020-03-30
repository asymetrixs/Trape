using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.Trading;

namespace trape.cli.trader
{
    class Program
    {
        public static IServiceProvider Services { get; private set; }

        static async Task Main(string[] args)
        {
            Configuration.SetUp();

            var app = CreateHostBuilder(args).Build();
            Services = app.Services;
            var logger = Services.GetRequiredService<ILogger>();

            logger.Information("Start up complete");

            try
            {
                await app.RunAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Error(e, e.Message);

                Console.Beep(700, 300);
                Console.Beep(300, 300);
                Console.Beep(700, 300);
                Console.Beep(300, 300);
                Console.Beep(700, 300);
                Console.Beep(300, 300);

                await Task.Delay(2000).ConfigureAwait(true);

                throw;
            }

            logger.Information("Shut down complete");
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration.Config).CreateLogger();

            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(configure => configure.AddSerilog())
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IBuffer, Cache.Buffer>();
                    services.AddSingleton<IAnalyst, Analyst>();
                    services.AddTransient<ITrader, Trader>();
                    services.AddSingleton<ITradingTeam, TradingTeam>();
                    services.AddSingleton<IAccountant, Accountant>();

                    services.AddSingleton<IBinanceClient>(new BinanceClient(new BinanceClientOptions()
                    {
                        ApiCredentials = new ApiCredentials(Configuration.GetValue("binance:apikey"),
                                                        Configuration.GetValue("binance:secretkey")),
                        LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Info,
                        LogWriters = new System.Collections.Generic.List<System.IO.TextWriter> { new BinanceLogger(Log.Logger) },
                        AutoTimestamp = true,
                        AutoTimestampRecalculationInterval = new TimeSpan(0, 5, 0),
                        TradeRulesBehaviour = TradeRulesBehaviour.AutoComply,
                        TradeRulesUpdateInterval = new TimeSpan(0, 5, 0)
                    }));

                    services.AddSingleton<IBinanceSocketClient>(new BinanceSocketClient(new BinanceSocketClientOptions()
                    {
                        ApiCredentials = new ApiCredentials(Configuration.GetValue("binance:apikey"),
                                                        Configuration.GetValue("binance:secretkey")),
                        AutoReconnect = true,
                        LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Info,
                        LogWriters = new System.Collections.Generic.List<System.IO.TextWriter> { new BinanceLogger(Log.Logger) }
                    }));

                    services.AddHostedService<Engine>();
                });
        }
    }
}
