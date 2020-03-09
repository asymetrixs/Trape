


using Serilog;
using System;

namespace trape.cli.collector
{
    public class CoinTrader
    {
        private ILogger _logger;

        public CoinTrader(ILogger logger)
        {
            this._logger = logger;
        }

        public void Run()
        {
            this._logger.Information("Started");

            while (true)
            {
                Console.WriteLine(DateTime.Now);
                System.Threading.Thread.Sleep(1000);
            }

        }
    }
}
