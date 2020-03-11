using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace trape.cli.trader
{
    public class Engine : BackgroundService
    {
        private ILogger _logger;

        private Cache.Buffer _buffer;

        private DecisionMaker _decisionMaker;

        public Engine(ILogger logger, Cache.Buffer buffer, DecisionMaker decisionMaker)
        {
            if(null == logger || null == buffer || null == decisionMaker)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._buffer = buffer;
            this._decisionMaker = decisionMaker;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Information("Engine is starting");

            await this._buffer.Start().ConfigureAwait(false);

            this._decisionMaker.Start();

            this._logger.Information("Engine is started");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.Information("Engine is stopping");

            this._decisionMaker.Stop();

            this._buffer.Stop();

            this._logger.Information("Engine is stopped");

            return Task.CompletedTask;
        }
    }
}
