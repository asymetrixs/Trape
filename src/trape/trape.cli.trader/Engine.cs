using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Cache;
using trape.cli.trader.Decision;
using trape.cli.trader.trade;

namespace trape.cli.trader
{
    public class Engine : BackgroundService
    {
        private ILogger _logger;

        private IBuffer _buffer;

        private IDecisionMaker _decisionMaker;

        private ITrader _trader;

        private IAccountant _accountant;

        public Engine(ILogger logger, IBuffer buffer, IDecisionMaker decisionMaker, ITrader trader, IAccountant accountant)
        {
            if(null == logger || null == buffer || null == decisionMaker || null == trader)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._buffer = buffer;
            this._decisionMaker = decisionMaker;
            this._trader = trader;
            this._accountant = accountant;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Information("Engine is starting");

            await this._buffer.Start().ConfigureAwait(false);

            this._decisionMaker.Start();

            await this._accountant.Start().ConfigureAwait(false);

            this._logger.Information("Engine is started");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.Information("Engine is stopping");

            this._accountant.Stop();

            this._decisionMaker.Stop();
                       
            this._buffer.Stop();

            this._logger.Information("Engine is stopped");

            return Task.CompletedTask;
        }
    }
}
