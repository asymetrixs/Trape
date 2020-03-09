using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.collector.DataCollection;
using trape.cli.collector.DataLayer;
using trape.jobs;

namespace trape.cli.collector
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
            }

            logger.Information("Shut down complete");
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            int rollingInterval = default;
            if (int.TryParse(Configuration.GetValue("Logging:FileRollingInterval"), out int ri))
            {
                rollingInterval = ri;
            }
            else
            {
                throw new System.ArgumentException("settings.json -> Logging:FileRollingInterval has an invalid value");
            }

            int? retainedFileCountLimit = default;
            if (string.IsNullOrEmpty(Configuration.GetValue("Logging:RetainedFileCountLimit")) || Configuration.GetValue("Logging:RetainedFileCountLimit") == "0")
            {
                retainedFileCountLimit = null;
            }
            else
            {
                if (int.TryParse(Configuration.GetValue("Logging:RetainedFileCountLimit"), out int rfcl))
                {
                    retainedFileCountLimit = rfcl;
                }
                else
                {
                    throw new System.ArgumentException("settings.json -> Logging:RetainedFileCountLimit has an invalid value");
                }
            }

            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .WriteTo.Async(a => a.File(
                    path: Configuration.GetValue("logging:filePath"),
                    rollingInterval: (RollingInterval)rollingInterval,
                    retainedFileCountLimit: retainedFileCountLimit))
                .CreateLogger();

            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(configure => configure.AddSerilog())
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILogger>(Log.Logger);
                    services.AddTransient<ICoinTradeContext, CoinTradeContext>();
                    services.AddSingleton<IJobManager, JobManager>();
                    services.AddHostedService<CollectionManager>();
                });
        }
    }
}
