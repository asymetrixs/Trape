using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.Fees;
using trape.cli.trader.Market;
using trape.cli.trader.Team;
using trape.cli.trader.Trading;
using trape.datalayer;
using Trape.BinanceNet.Logger;

namespace trape.cli.trader
{
    class Program
    {
        #region Properties

        /// <summary>
        /// DI/IoC Container
        /// </summary>
        public static Container Container { get; private set; }

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
            var app = CreateHost();

            var logger = Container.GetInstance<ILogger>().ForContext<Program>();
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
        public static IHost CreateHost()
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
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
                    outputTemplate: Config.GetValue("Serilog:OutputTemplateConsole")
                )
                .WriteTo.File(
                    path: Config.GetValue("Serilog:LogFileLocation"), retainedFileCountLimit: 7, rollingInterval: RollingInterval.Day, buffered: false,
                    outputTemplate: Config.GetValue("Serilog:OutputTemplateFile")
                )
                .CreateLogger();

            var dbContextOptionsBuilder = new DbContextOptionsBuilder<TrapeContext>();
            dbContextOptionsBuilder.UseNpgsql(Config.GetConnectionString("trape-db"));
            dbContextOptionsBuilder.EnableDetailedErrors(false);
            dbContextOptionsBuilder.EnableSensitiveDataLogging(false);

            // Setup container and register defauld scope as first thing
            Container = new Container();
            Container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(configure => configure.AddSerilog(Log.Logger))
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true, ValidateScopes = true });
                    services.AddLogging();

                    services.AddSimpleInjector(Container, options =>
                    {
                        options.AddHostedService<Engine>();
                        options.AddLogging();
                    });
                })
                .UseConsoleLifetime()
                .Build()
                .UseSimpleInjector(Container);

            // Registration

            Container.Register<TrapeContext, TrapeContext>(Lifestyle.Scoped);
            
            Container.Register<IStockExchange, StockExchange>(Lifestyle.Transient);
            Container.Register<IBroker, Broker>(Lifestyle.Transient);
            Container.Register<IAnalyst, Analyst>(Lifestyle.Transient);

            Container.Register<IBuffer, Cache.Buffer>(Lifestyle.Singleton);
            Container.Register<IAccountant, Accountant>(Lifestyle.Singleton);
            Container.Register<IFeeWatchdog, FeeWatchdog>(Lifestyle.Singleton);
            Container.Register<ITradingTeam, TradingTeam>(Lifestyle.Singleton);
            Container.Register<ITrapeContextCreator, TrapeContextDiCreator>(Lifestyle.Singleton);


            Container.RegisterInstance(Log.Logger);
            Container.RegisterInstance(Config.Current);
            Container.RegisterInstance(dbContextOptionsBuilder);
            Container.RegisterInstance(dbContextOptionsBuilder.Options);

            Container.RegisterInstance<IBinanceClient>(new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(Config.GetValue("binance:apikey"),
                                                Config.GetValue("binance:secretkey")),
                LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Debug,
                LogWriters = new System.Collections.Generic.List<System.IO.TextWriter> { new Logger(Log.Logger) },
                AutoTimestamp = true,
                AutoTimestampRecalculationInterval = new TimeSpan(0, 5, 0),
                TradeRulesBehaviour = TradeRulesBehaviour.AutoComply,
                TradeRulesUpdateInterval = new TimeSpan(0, 5, 0)
            }));

            Container.RegisterInstance<IBinanceSocketClient>(new BinanceSocketClient(new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials(Config.GetValue("binance:apikey"),
                                                Config.GetValue("binance:secretkey")),
                AutoReconnect = true,
                LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Info,
                LogWriters = new System.Collections.Generic.List<System.IO.TextWriter> { new Logger(Log.Logger) }
            }));

            Container.Verify();

            return host;
        }
    }

    #endregion
}
