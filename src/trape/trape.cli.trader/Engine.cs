using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache;

namespace trape.cli.trader
{
    public class Engine : BackgroundService
    {
        private ILogger _logger;

        private Synchronizer _synchronizer;

        public Engine(ILogger logger, Synchronizer synchronizer)
        {
            if(null == logger || null == _synchronizer)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._synchronizer = synchronizer;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._synchronizer.Start();

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this._synchronizer.Stop();

            return Task.CompletedTask;
        }
    }
}
