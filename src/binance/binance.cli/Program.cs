using Microsoft.Extensions.Configuration;
using System;

namespace binance.cli
{
    class Program
    {
        public static IConfigurationRoot Configuration;

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            Console.WriteLine("Hello World!");
        }
    }
}
