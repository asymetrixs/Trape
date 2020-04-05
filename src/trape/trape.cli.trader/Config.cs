using Microsoft.Extensions.Configuration;
using System;

namespace trape.cli.trader
{
    public static class Config
    {
        public static IConfigurationRoot Current { get; private set; }

        public static void SetUp()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true);

            Current = builder.Build();

            Pool.Initialize();
        }

        public static string GetValue(string section)
        {
            return Current.GetSection(section).Value;
        }

        public static string GetConnectionString(string connectionName)
        {
            return Current.GetConnectionString(connectionName);
        }
    }
}
