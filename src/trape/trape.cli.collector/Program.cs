using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Threading.Tasks;
using trape.cli.collector.DataCollection;
using trape.datalayer;
using trape.jobs;
using Trape.BinanceNet.Logger;

namespace trape.cli.collector
{
    class Program
    {
        #region Properties

        /// <summary>
        /// Services
        /// </summary>
        public static IServiceProvider Services { get; private set; }

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

            // Setup IoC container
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
            catch (OperationCanceledException oce)
            {
                // Ignore
                logger.Warning(oce, oce.Message);
            }
            catch (Exception e)
            {
                logger.Error(e, e.Message);
            }

            Pool.DatabasePool.ClearUnused();

            logger.Information("Shut down complete");
        }

        /// <summary>
        /// Creates the IoC container
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

            var dbContextOptionsBuilder = new DbContextOptionsBuilder<TrapeContext>();
            dbContextOptionsBuilder.UseNpgsql(Config.GetConnectionString("trape-db"));
            dbContextOptionsBuilder.EnableDetailedErrors(false);
            dbContextOptionsBuilder.EnableSensitiveDataLogging(false);

            // Register classes/interfaces in IoC
            return Host.CreateDefaultBuilder()
                .ConfigureLogging(configure => configure.AddSerilog(Log.Logger))
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<ITrapeContextCreator, TrapeContextDiCreator>();
                    services.AddSingleton(dbContextOptionsBuilder.Options);
                    services.AddDbContext<TrapeContext>();
                    
                    services.AddSingleton(Config.Current);
                    //services.AddTransient<ITrapeContext, TrapeContext>();
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IJobManager, JobManager>();
                    services.AddHostedService<CollectionManager>();

                    services.AddSingleton<IBinanceSocketClient>(new BinanceSocketClient(new BinanceSocketClientOptions()
                    {
                        ApiCredentials = new ApiCredentials(Config.GetValue("binance:apikey"),
                                                        Config.GetValue("binance:secretkey")),
                        AutoReconnect = true,
                        LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Info,
                        LogWriters = new System.Collections.Generic.List<System.IO.TextWriter> { new Logger(Log.Logger) }
                    }));
                });
        }
    }
}
