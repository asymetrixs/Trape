using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.Fees;
using trape.cli.trader.Trading;
using trape.cli.trader.WatchDog;

namespace trape.cli.trader
{
    /// <summary>
    /// The Engine manages proper startup and shutdown of required services
    /// </summary>
    public class Engine : BackgroundService
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Buffer
        /// </summary>
        private IBuffer _buffer;

        /// <summary>
        /// Analyst
        /// </summary>
        private IAnalyst _analyst;

        /// <summary>
        /// Trading Team
        /// </summary>
        private ITradingTeam _tradingTeam;

        /// <summary>
        /// Accountant
        /// </summary>
        private IAccountant _accountant;

        /// <summary>
        /// Fee Watchdog
        /// </summary>
        private IFeeWatchdog _feeWatchdog;

        /// <summary>
        /// Checker
        /// </summary>
        private IWarden _checker;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Engine</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="buffer">Buffer</param>
        /// <param name="analyst">Analyst</param>
        /// <param name="tradingTeam">Trading Team</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="feeWatchdog">Fee Watchdog</param>
        /// <param name="checker">Checker</param>
        public Engine(ILogger logger, IBuffer buffer, IAnalyst analyst, ITradingTeam tradingTeam, IAccountant accountant, IFeeWatchdog feeWatchdog, IWarden checker)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _ = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _ = analyst ?? throw new ArgumentNullException(paramName: nameof(analyst));

            _ = tradingTeam ?? throw new ArgumentNullException(paramName: nameof(tradingTeam));

            _ = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            _ = feeWatchdog ?? throw new ArgumentNullException(paramName: nameof(feeWatchdog));

            _ = checker ?? throw new ArgumentNullException(paramName: nameof(checker));

            #endregion

            this._logger = logger.ForContext<Engine>();
            this._buffer = buffer;
            this._analyst = analyst;
            this._tradingTeam = tradingTeam;
            this._accountant = accountant;
            this._feeWatchdog = feeWatchdog;
            this._checker = checker;
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Starts all processes to begin trading
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Information("Engine is starting");

            await this._buffer.Start().ConfigureAwait(true);

            this._analyst.Start();

            await this._accountant.Start().ConfigureAwait(true);

            this._tradingTeam.Start();

            this._feeWatchdog.Start();

            var checker = Program.Container.GetInstance<IWarden>();
            checker.Start();

            this._logger.Information("Engine is started");
        }

        /// <summary>
        /// Stops all process to end trading
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async override Task StopAsync(CancellationToken cancellationToken = default)
        {
            this._logger.Information("Engine is stopping");

            var checker = Program.Container.GetInstance<IWarden>();
            checker.Terminate();

            this._feeWatchdog.Terminate();

            await this._tradingTeam.Finish().ConfigureAwait(true);

            await this._accountant.Finish().ConfigureAwait(true);

            this._analyst.Finish();

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
                this._analyst.Dispose();
                this._tradingTeam.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
