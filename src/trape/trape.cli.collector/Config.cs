using Microsoft.Extensions.Configuration;
using System;

namespace trape.cli.collector
{
    /// <summary>
    /// This class holds the configuration
    /// </summary>
    public static class Config
    {
        #region Properties

        /// <summary>
        /// Holds current reflection of settings.json
        /// </summary>
        public static IConfigurationRoot Current { get; private set; }

        #endregion

        /// <summary>
        /// Initializes the configuration
        /// </summary>
        public static void SetUp()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true);

            Current = builder.Build();

            Pool.Initialize();
        }

        /// <summary>
        /// Returns a value from settings.con
        /// </summary>
        /// <param name="section">The identifier of the value</param>
        /// <returns></returns>
        public static string GetValue(string section)
        {
            return Current.GetSection(section).Value;
        }

        /// <summary>
        /// Returns a connection string
        /// </summary>
        /// <param name="connectionName">The identifier of the connection string</param>
        /// <returns></returns>
        public static string GetConnectionString(string connectionName)
        {
            return Current.GetConnectionString(connectionName);
        }
    }
}
