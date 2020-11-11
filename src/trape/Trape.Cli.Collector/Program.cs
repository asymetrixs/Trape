using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot;
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
using Trape.BinanceNet.Logger;
using Trape.Cli.collector.DataCollection;
using Trape.Datalayer;
using Trape.Jobs;

namespace Trape.Cli.collector
{
    class Program
    {
        #region Properties

        /// <summary>
        /// DI/IoC Container
        /// </summary>
        public static Container Container { get; private set; }

        #endregion

        /// <summary>
        /// Main starting point
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            // Setup configuration
            Config.SetUp();

            ILogger logger;

            // Setup IoC container
            using (var app = CreateHost())
            {
                logger = Container.GetInstance<ILogger>().ForContext<Program>();
                logger.Information("Starting...");

                try
                {
                    // Run App
                    await app.RunAsync().ConfigureAwait(true);

                    logger.Information("Terminating...");
                }
                catch (OperationCanceledException oce)
                {
                    // Ignore
                    logger.Warning(oce, oce.Message);
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
            }

            logger.Information("Shut down complete");

            // Exit code will be 0, clean exit
            Environment.ExitCode = 0;
        }

        /// <summary>
        /// Creates the IoC container
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
#if DEBUG
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
#else
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
#endif
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

            // Register classes/interfaces in IoC
            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(configure => configure.AddSerilog(Log.Logger))
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true, ValidateScopes = true });
                    services.AddLogging();

                    services.AddSimpleInjector(Container, options =>
                    {
                        options.AddHostedService<CollectionManager>();
                        options.AddLogging();
                    });
                })
                .UseConsoleLifetime()
                .Build()
                .UseSimpleInjector(Container);

            // Registration
            Container.Register<TrapeContext, TrapeContext>(Lifestyle.Scoped);

            Container.Register<IJobManager, JobManager>(Lifestyle.Singleton);
            Container.Register<ITrapeContextCreator, TrapeContextDiCreator>(Lifestyle.Singleton);

            Container.RegisterInstance(Log.Logger);
            Container.RegisterInstance(Config.Current);
            Container.RegisterInstance(dbContextOptionsBuilder);
            Container.RegisterInstance(dbContextOptionsBuilder.Options);

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
}
