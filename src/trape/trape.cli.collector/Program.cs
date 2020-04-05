using Binance.Net;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading.Tasks;
using trape.cli.collector.DataCollection;
using trape.jobs;
using Trape.BinanceNet.Logger;

namespace trape.cli.collector
{
    class Program
    {
        public static IServiceProvider Services { get; private set; }

        static async Task Main(string[] args)
        {
            Config.SetUp();

            var app = CreateHostBuilder(args).Build();
            Services = app.Services;
            var logger = Services.GetRequiredService<ILogger>().ForContext<Program>();            
            logger.Information("Start up complete");

            try
            {
                await app.RunAsync().ConfigureAwait(false);
            }
            catch(OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.Error(e, e.Message);
            }

            Pool.DatabasePool.ClearUnused();

            logger.Information("Shut down complete");
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Config.Current).CreateLogger();

            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(configure => configure.AddSerilog())
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILogger>(Log.Logger);
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
