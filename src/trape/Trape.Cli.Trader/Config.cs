namespace Trape.Cli.Trader
{
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// This class holds settings.json
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Current settings.json
        /// </summary>
        public static IConfigurationRoot Current { get; private set; }

        /// <summary>
        /// Initializes the configuration from settings.json
        /// </summary>
        public static void SetUp()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true);

            Current = builder.Build();
        }

        /// <summary>
        /// Returns a value from settings.json
        /// </summary>
        /// <param name="path">Path to value</param>
        /// <returns></returns>
        public static string GetValue(string path)
        {
            return Current.GetSection(path).Value;
        }
    }
}
