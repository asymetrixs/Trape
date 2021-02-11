namespace Trape.Cli.Trader
{
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Trape.Cli.Trader.Account;
    using Trape.Cli.Trader.Fees;
    using Trape.Cli.Trader.Listener;
    using Trape.Cli.Trader.Team;

    /// <summary>
    /// The Engine manages proper startup and shutdown of required services
    /// </summary>
    public class Engine : BackgroundService
    {
        /// <summary>
        /// Running waitfor
        /// </summary>
        private readonly SemaphoreSlim _running;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Listener
        /// </summary>
        private readonly IListener _listener;

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
        /// Initializes a new instance of the <c>Engine</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="buffer">Buffer</param>
        /// <param name="tradingTeam">Trading Team</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="feeWatchdog">Fee Watchdog</param>
        public Engine(ILogger logger, IListener buffer, ITradingTeam tradingTeam, IAccountant accountant, IFeeWatchdog feeWatchdog)
        {
            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._listener = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            this._tradingTeam = tradingTeam ?? throw new ArgumentNullException(paramName: nameof(tradingTeam));

            this._accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            this._feeWatchdog = feeWatchdog ?? throw new ArgumentNullException(paramName: nameof(feeWatchdog));

            this._logger = logger.ForContext<Engine>();
            this._running = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Starts all processes to begin trading
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            this._logger.Information("Engine is starting");

            this._running.Wait(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await this._listener.Start().ConfigureAwait(true);

            await this._accountant.Start().ConfigureAwait(true);

            this._tradingTeam.Start();

            this._feeWatchdog.Start();

            this._logger.Information("Engine is started");
        }

        /// <summary>
        /// Stops all process to end trading
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        public override async Task StopAsync(CancellationToken cancellationToken = default)
        {
            this._logger.Information("Engine is stopping");

            this._feeWatchdog.Terminate();

            this._tradingTeam.Terminate();

            await this._accountant.Terminate().ConfigureAwait(true);

            // End running state
            this._running.Release();

            this._listener.Terminate();

            this._logger.Information("Engine is stopped");
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public override void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);

            base.Dispose();
        }

        /// <summary>
        /// Waits for process
        /// </summary>
        /// <param name="stoppingToken">Stopping token</param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken = default)
        {
            this._logger.Verbose("Waiting...");

            // Return state
            return this._running.WaitAsync(stoppingToken);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._listener.Dispose();
                this._accountant.Dispose();
                this._tradingTeam.Dispose();
                this._running.Dispose();
            }

            this._disposed = true;
        }
    }
}
