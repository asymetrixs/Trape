using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Exceptions;
using System.Threading;
using trape.cli.collector.DataCollection;
using trape.cli.collector.DataLayer;

namespace trape.cli.collector
{
    public static class Service
    {
        private static ServiceProvider _services;

        /// <summary>
        /// Set up of the service provider
        /// </summary>
        /// <returns></returns>
        public static void SetUp(CancellationToken cancellationToken)
        {
            // Set up dependency injection
            var serviceCollection = new ServiceCollection();


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

            serviceCollection.AddLogging(configure => configure.AddSerilog());
            serviceCollection.AddSingleton<ILogger>(Log.Logger);
            
            // Add cointrade application
            serviceCollection.AddTransient<ICollectionManager, CollectionManager>();
            serviceCollection.AddSingleton<IKillSwitch>(new KillSwitch(cancellationToken));
            serviceCollection.AddTransient<ICoinTradeContext, CoinTradeContext>();
            

            _services = serviceCollection.BuildServiceProvider();

            _services.GetService<ILogger>().Verbose("Services have been configured");
        }

        /// <summary>
        /// Set up of the service provider
        /// </summary>
        /// <param name="serviceProvider">The preconfigured service provider.</param>
        /// <returns></returns>
        public static void SetUp(ServiceProvider serviceProvider)
        {
            _services = serviceProvider;
        }

        /// <summary>
        /// Locates and returns the requested service.
        /// </summary>
        /// <param name="T">The type that was used to register the service.</param>
        /// <returns>Returns the requested service if found.</returns>
        public static T Get<T>()
        {
            return _services.GetService<T>();
        }
    }
}
