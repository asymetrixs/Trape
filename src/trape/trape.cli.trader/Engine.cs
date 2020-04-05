using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.Trading;

namespace trape.cli.trader
{
    public class Engine : BackgroundService
    {
        #region Fields

        private ILogger _logger;

        private IBuffer _buffer;

        private IAnalyst _recommender;

        private ITradingTeam _tradingTeam;

        private IAccountant _accountant;

        private bool _disposed;

        #endregion

        #region Constructor

        public Engine(ILogger logger, IBuffer buffer, IAnalyst recommender, ITradingTeam tradingTeam, IAccountant accountant)
        {
            if (null == logger || null == buffer || null == recommender || null == tradingTeam)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger.ForContext<Engine>();
            this._buffer = buffer;
            this._recommender = recommender;
            this._tradingTeam = tradingTeam;
            this._accountant = accountant;
        }

        #endregion

        #region Start / Stop

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Information("Engine is starting");

            await this._buffer.Start().ConfigureAwait(true);

            this._recommender.Start();

            await this._accountant.Start().ConfigureAwait(true);

            this._tradingTeam.Start();

            this._logger.Information("Engine is started");
        }

        public async override Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.Information("Engine is stopping");

            await this._tradingTeam.Finish().ConfigureAwait(true);

            await this._accountant.Finish().ConfigureAwait(true);

            this._recommender.Finish();

            this._buffer.Finish();

            this._logger.Information("Engine is stopped");
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public override void Dispose()
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
                this._tradingTeam.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
