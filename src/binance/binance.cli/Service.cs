using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace binance.cli
{
    public static class Service
    {
        public static ServiceProvider Services { get; set; }

        /// <summary>
        /// Set up of the service provider
        /// </summary>
        /// <returns></returns>
        public static void SetUp()
        {
            // Set up dependency injection
            var serviceCollection = new ServiceCollection();

            //// Process Proxy
            //serviceCollection.AddTransient<IProcessProxy, ProcessProxy>();

            //// File Provider
            //serviceCollection.AddSingleton<IFilesystemProxy, FilesystemProxy>();

            // Logger
            var logFileLocation = default(string);

            //if (!Directory.Exists(FileOperation.VAR_LOG_NISP))
            //{
            //    Directory.CreateDirectory(FileOperation.VAR_LOG_NISP);
            //}


            //var logger = new LoggerConfiguration()
            //    .MinimumLevel.Verbose()
            //    .WriteTo.File(logFileLocation, rollingInterval: RollingInterval.Day)
            //    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information,
            //                        outputTemplate: "[{Level:u4}] {Message:lj}{NewLine}{Exception}",
            //                        theme: ConsoleTheme.None)
            //    .CreateLogger();
            // Logger
            //serviceCollection.AddSingleton(typeof(ILogger), logger);

            // SystemEnv
            //serviceCollection.AddSingleton(typeof(ISystemEnv), typeof(SystemEnv));

            Services = serviceCollection.BuildServiceProvider();

        }

        /// <summary>
        /// Set up of the service provider
        /// </summary>
        /// <param name="serviceProvider">The preconfigured service provider.</param>
        /// <returns></returns>
        public static void SetUp(ServiceProvider serviceProvider)
        {
            Services = serviceProvider;
        }

        /// <summary>
        /// Locates and returns the requested service.
        /// </summary>
        /// <param name="T">The type that was used to register the service.</param>
        /// <returns>Returns the requested service if found.</returns>
        public static T GetService<T>()
        {
            return Services.GetService<T>();
        }
    }
}
