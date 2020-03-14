using Microsoft.Extensions.Configuration;
using System;

namespace trape.cli.collector
{
    public static class Configuration
    {
        public static IConfigurationRoot Config { get; private set; }
        
        public static void SetUp()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true);

            Config = builder.Build();
        }

        public static string GetValue(string section)
        {
            return Config.GetSection(section).Value;
        }

        public static string GetConnectionString(string connectionName)
        {
            return Config.GetConnectionString(connectionName);
        }
    }
}
