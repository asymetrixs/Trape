using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.trader.Account;
using Trape.Cli.trader.Listener;
using Trape.Cli.trader.Fees;
using Trape.Cli.trader.Team;

namespace Trape.Cli.trader
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
        private readonly ILogger _logger;

        /// <summary>
        /// Buffer
        /// </summary>
        private readonly IListener _buffer;

        /// <summary>
        /// Trading Team
        /// </summary>
        private readonly ITradingTeam _tradingTeam;

        /// <summary>
        /// Accountant
        /// </summary>
        private readonly IAccountant _accountant;

        /// <summary>
        /// Fee Watchdog
        /// </summary>
        private readonly IFeeWatchdog _feeWatchdog;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Running waitfor
        /// </summary>
        private readonly SemaphoreSlim _running;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Engine</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="buffer">Buffer</param>
        /// <param name="tradingTeam">Trading Team</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="feeWatchdog">Fee Watchdog</param>
        public Engine(ILogger logger, IListener buffer, ITradingTeam tradingTeam, IAccountant accountant, IFeeWatchdog feeWatchdog)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _tradingTeam = tradingTeam ?? throw new ArgumentNullException(paramName: nameof(tradingTeam));

            _accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            _feeWatchdog = feeWatchdog ?? throw new ArgumentNullException(paramName: nameof(feeWatchdog));

            #endregion

            _logger = logger.ForContext<Engine>();
            _running = new SemaphoreSlim(1, 1);
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Starts all processes to begin trading
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.Information("Engine is starting");

            _running.Wait(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await _buffer.Start().ConfigureAwait(true);

            await _accountant.Start().ConfigureAwait(true);

            _tradingTeam.Start();

            _feeWatchdog.Start();

            _logger.Information("Engine is started");
        }

        /// <summary>
        /// Waits for process
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken = default)
        {
            _logger.Verbose("Waiting...");

            // Return state
            return _running.WaitAsync(stoppingToken);
        }

        /// <summary>
        /// Stops all process to end trading
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.Information("Engine is stopping");

            _feeWatchdog.Terminate();

            _tradingTeam.Terminate();

            await _accountant.Terminate().ConfigureAwait(true);

            // End running state
            _running.Release();

            _buffer.Terminate();

            _logger.Information("Engine is stopped");
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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _buffer.Dispose();
                _accountant.Dispose();
                _tradingTeam.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
