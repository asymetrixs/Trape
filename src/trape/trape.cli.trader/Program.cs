using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.Trading;
using Trape.BinanceNet.Logger;

namespace trape.cli.trader
{
    class Program
    {
        #region Properties

        /// <summary>
        /// Services
        /// </summary>
        public static IServiceProvider Services { get; private set; }

        #endregion

        #region Function

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            // Set up configuration
            Config.SetUp();

            // Create IoC container
            var app = CreateHostBuilder().Build();
            Services = app.Services;

            // Initialize pool
            Pool.Initialize();

            var logger = Services.GetRequiredService<ILogger>().ForContext<Program>();
            logger.Information("Start up complete");

            try
            {
                // Run App
                await app.RunAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Error(e, e.Message);

                // Wait 2 seconds
                await Task.Delay(2000).ConfigureAwait(true);

                // Signal unclean exit and rely on service manager (e.g. systemd) to restart the service
                logger.Warning("Check previous errors. Exiting with 254.");
                Environment.Exit(254);
            }

            logger.Information("Shut down complete");

            // Exit code will be 0, clean exit
            Environment.ExitCode = 0;
        }

        /// <summary>
        /// Creates the host builder
        /// </summary>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder()
        {
            // Configure Logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Config.Current)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Destructure.ToMaximumDepth(4)
                .Destructure.ToMaximumStringLength(100)
                .Destructure.ToMaximumCollectionCount(10)
                .WriteTo.Console(
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                    outputTemplate: Config.GetValue("Serilog:OutputTemplateConsole")
                )
                .WriteTo.File(
                    path: Config.GetValue("Serilog:LogFileLocation"), retainedFileCountLimit: 7, rollingInterval: RollingInterval.Day, buffered: false,
                    outputTemplate: Config.GetValue("Serilog:OutputTemplateFile")
                )
                .CreateLogger();


            return Host.CreateDefaultBuilder()
                .ConfigureLogging(configure => configure.AddSerilog())
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<ITrapeContext, TrapeContext>();
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IBuffer, Cache.Buffer>();
                    services.AddSingleton<IAnalyst, Analyst>();
                    services.AddTransient<IBroker, Broker>();
                    services.AddSingleton<ITradingTeam, TradingTeam>();
                    services.AddSingleton<IAccountant, Accountant>();

                    services.AddSingleton<IBinanceClient>(new BinanceClient(new BinanceClientOptions()
                    {
                        ApiCredentials = new ApiCredentials(Config.GetValue("binance:apikey"),
                                                        Config.GetValue("binance:secretkey")),
                        LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Info,
                        LogWriters = new System.Collections.Generic.List<System.IO.TextWriter> { new Logger(Log.Logger) },
                        AutoTimestamp = true,
                        AutoTimestampRecalculationInterval = new TimeSpan(0, 5, 0),
                        TradeRulesBehaviour = TradeRulesBehaviour.AutoComply,
                        TradeRulesUpdateInterval = new TimeSpan(0, 5, 0)
                    }));

                    services.AddSingleton<IBinanceSocketClient>(new BinanceSocketClient(new BinanceSocketClientOptions()
                    {
                        ApiCredentials = new ApiCredentials(Config.GetValue("binance:apikey"),
                                                        Config.GetValue("binance:secretkey")),
                        AutoReconnect = true,
                        LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Info,
                        LogWriters = new System.Collections.Generic.List<System.IO.TextWriter> { new Logger(Log.Logger) }
                    }));

                    services.AddHostedService<Engine>();
                });
        }
    }

    #endregion
}
