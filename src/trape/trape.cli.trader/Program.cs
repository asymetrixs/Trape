using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using System;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Cache;
using trape.cli.trader.DataLayer;
using trape.cli.trader.Decision;
using trape.cli.trader.trade;

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
                    services.AddSingleton<ILogger>(Log.Logger);
                    services.AddSingleton<IBuffer, Cache.Buffer>();
                    services.AddSingleton<IDecisionMaker, DecisionMaker>();
                    services.AddSingleton<ITrader, Trader>();
                    services.AddSingleton<IAccountant, Accountant>();

                    services.AddTransient<ITrapeContext, TrapeContext>();
                    
                    services.AddHostedService<Engine>();
                });
        }
    }
}
