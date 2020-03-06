using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using log4net;
using log4net.Core;
using log4net.Config;

namespace binance.cli
{
    public static class Service
    {
        private static ServiceProvider _services;

        /// <summary>
        /// Set up of the service provider
        /// </summary>
        /// <returns></returns>
        public static void SetUp()
        {
            // Set up dependency injection
            var serviceCollection = new ServiceCollection();
            
                        
            // SystemEnv
            //serviceCollection.AddSingleton(typeof(ISystemEnv), typeof(SystemEnv));

            _services = serviceCollection.BuildServiceProvider();

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
