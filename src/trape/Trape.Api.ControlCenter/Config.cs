using Microsoft.Extensions.Configuration;
using System;

namespace Trape.Api.ControlCenter
{
    /// <summary>
    /// This class holds settings.json
    /// </summary>
    public static class Config
    {
        #region Properties

        /// <summary>
        /// Current settings.json
        /// </summary>
        public static IConfigurationRoot Current { get; private set; }

        #endregion

        #region

        /// <summary>
        /// Initializes the configuration from settings.json
        /// </summary>
        public static void SetUp()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Current = builder.Build();
        }

        public static string GetValue(string section)
        {
            return Current.GetSection(section).Value;
        }

        public static string GetConnectionString(string connectionName)
        {
            return Current.GetConnectionString(connectionName);
        }

        #endregion
    }
}
