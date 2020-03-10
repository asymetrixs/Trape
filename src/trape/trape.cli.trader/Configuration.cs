using Microsoft.Extensions.Configuration;
using System;

namespace trape.cli.trader
{
    public static class Configuration
    {
        private static IConfigurationRoot _configuration;

        public static void SetUp()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
        }

        public static string GetValue(string section)
        {
            return _configuration.GetSection(section).Value;
        }

        public static string GetConnectionString(string connectionName)
        {
            return _configuration.GetConnectionString(connectionName);
        }
    }
}
