using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.trade;

namespace trape.cli.trader
{
    public class Engine : BackgroundService
    {
        #region Fields

        private ILogger _logger;

        private IBuffer _buffer;

        private IRecommender _recommender;

        private ITrader _trader;

        private IAccountant _accountant;

        private bool _disposed;

        #endregion

        public Engine(ILogger logger, IBuffer buffer, IRecommender recommender, ITrader trader, IAccountant accountant)
        {
            if (null == logger || null == buffer || null == recommender || null == trader)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._buffer = buffer;
            this._recommender = recommender;
            this._trader = trader;
            this._accountant = accountant;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Information("Engine is starting");

            await this._buffer.Start().ConfigureAwait(true);

            this._recommender.Start();

            await this._accountant.Start().ConfigureAwait(true);

            this._trader.Start();

            this._logger.Information("Engine is started");
        }

        public async override Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.Information("Engine is stopping");

            await this._trader.Stop().ConfigureAwait(true);

            await this._accountant.Stop().ConfigureAwait(true);

            this._recommender.Stop();

            this._buffer.Stop();

            this._logger.Information("Engine is stopped");
        }

        #region Dispose

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._buffer.Dispose();
                this._accountant.Dispose();
                this._recommender.Dispose();
                this._trader.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
