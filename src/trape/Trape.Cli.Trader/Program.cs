namespace Trape.Cli.Trader
{
    using Binance.Net;
    using Binance.Net.Enums;
    using Binance.Net.Interfaces;
    using Binance.Net.Objects.Spot;
    using CryptoExchange.Net.Authentication;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using Serilog.Exceptions;
    using SimpleInjector;
    using SimpleInjector.Diagnostics;
    using SimpleInjector.Lifestyles;
    using System;
    using System.Threading.Tasks;
    using Trape.BinanceNet.Logger;
    using Trape.Cli.Trader.Account;
    using Trape.Cli.Trader.Cache;
    using Trape.Cli.Trader.Fees;
    using Trape.Cli.Trader.Listener;
    using Trape.Cli.Trader.Market;
    using Trape.Cli.Trader.Team;

    public static class Program
    {
        /// <summary>
        /// Binance Client
        /// </summary>
        private static BinanceClient _binanceClient = new BinanceClient();

        /// <summary>
        /// Binance Socket Client
        /// </summary>
        private static BinanceSocketClient _binanceSocketClient = new BinanceSocketClient();

        /// <summary>
        /// DI/IoC Container
        /// </summary>
        public static Container Container { get; } = new Container();

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <returns></returns>
        public static async Task Main()
        {
            // Set up configuration
            Config.SetUp();

            ILogger logger;

            // Create IoC container
            using (var app = CreateHost())
            {
                logger = Container.GetInstance<ILogger>().ForContext(typeof(Program));

                logger.Information("Starting...");

                try
                {
                    // Run App
                    await app.RunAsync().ConfigureAwait(true);

                    logger.Information("Terminating...");
                }
                catch (Exception e)
                {
                    logger.Error(e, e.Message);

                    // Wait 2 seconds
                    await Task.Delay(2000).ConfigureAwait(true);

                    // Signal unclean exit and rely on service manager (e.g. systemd) to restart the service
                    logger.Warning("Check previous errors. Exiting with 254.");
                    await Task.Delay(100).ConfigureAwait(true);
                    throw;
                }
            }

            // Dispose
            _binanceClient.Dispose();
            _binanceSocketClient.Dispose();

            logger.Information("Shut down complete");
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
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
                    outputTemplate: Config.GetValue("Serilog:OutputTemplateConsole"))
                .WriteTo.File(
                    path: Config.GetValue("Serilog:LogFileLocation"),
                    retainedFileCountLimit: 7,
                    rollingInterval: RollingInterval.Day,
                    buffered: false,
                    outputTemplate: Config.GetValue("Serilog:OutputTemplateFile"))
                .CreateLogger();

            // Setup container and register defauld scope as first thing
            Container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(configure => configure.AddSerilog(Log.Logger))
                .UseSystemd()
                .ConfigureServices((_, services) =>
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
            Container.Register<IBroker, Broker>(Lifestyle.Transient);
            Container.Register<IAnalyst, Analyst>(Lifestyle.Transient);

            var registration = Container.GetRegistration(typeof(IBroker))?.Registration;
            registration?.SuppressDiagnosticWarning(DiagnosticType.DisposableTransientComponent, "Application takes care of disposal.");
            registration = Container.GetRegistration(typeof(IAnalyst))?.Registration;
            registration?.SuppressDiagnosticWarning(DiagnosticType.DisposableTransientComponent, "Application takes care of disposal.");

            Container.Register<IStockExchange, StockExchange>(Lifestyle.Singleton);
            Container.Register<IListener, Listener.Listener>(Lifestyle.Singleton);
            Container.Register<IStore, Trader.Cache.Store>(Lifestyle.Singleton);
            Container.Register<IAccountant, Accountant>(Lifestyle.Singleton);
            Container.Register<IFeeWatchdog, FeeWatchdog>(Lifestyle.Singleton);
            Container.Register<ITradingTeam, TradingTeam>(Lifestyle.Singleton);

            Container.RegisterInstance(Log.Logger);
            Container.RegisterInstance(Config.Current);

            // Set up Binance Client
            var apiCredentials = new ApiCredentials(Config.GetValue("binance:apikey"),
                                                Config.GetValue("binance:secretkey"));
            _binanceClient = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = apiCredentials,
                LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Debug,
                LogWriters = new System.Collections.Generic.List<System.IO.TextWriter> { new BinanceLoggerAdapter(Log.Logger) },
                AutoTimestamp = true,
                AutoTimestampRecalculationInterval = new TimeSpan(0, 5, 0),
                TradeRulesBehaviour = TradeRulesBehaviour.AutoComply,
                TradeRulesUpdateInterval = new TimeSpan(0, 5, 0),
            });

            Container.RegisterInstance<IBinanceClient>(_binanceClient);

            // Set up Binance Socket Client
            _binanceSocketClient = new BinanceSocketClient(new BinanceSocketClientOptions()
            {
                ApiCredentials = apiCredentials,
                AutoReconnect = true,
                LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Info,
                LogWriters = new System.Collections.Generic.List<System.IO.TextWriter> { new BinanceLoggerAdapter(Log.Logger) },
            });

            Container.RegisterInstance<IBinanceSocketClient>(_binanceSocketClient);

            Container.Verify();

            return host;
        }
    }
}
