using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trape.cli.trader.Cache;
using Microsoft.Extensions.DependencyInjection;
using trape.cli.trader.WatchDog;
using trape.datalayer;
using trape.jobs;
using System.Threading;

namespace trape.cli.trader.Trading
{
    /// <summary>
    /// Starts, stops, and manages the <c>Brokers</c>
    /// </summary>
    public class TradingTeam : ITradingTeam, IActive
    {
        #region Fields

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Team holding the <c>Broker</c>s
        /// </summary>
        private readonly List<IBroker> _team;

        /// <summary>
        /// Buffer
        /// </summary>
        private readonly IBuffer _buffer;

        /// <summary>
        /// Job to check for new/obsolete Symbols
        /// </summary>
        private readonly Job _jobBrokerCheck;

        /// <summary>
        /// Synchronizer
        /// </summary>
        private readonly SemaphoreSlim _brokerSynchronizer;

        /// <summary>
        /// Last active
        /// </summary>
        public DateTime LastActive { get; private set; }

        #endregion

        #region Constructor 

        /// <summary>
        /// Initializes a new instance of the <c>TradingTeam</c> class.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="buffer"></param>
        public TradingTeam(ILogger logger, IBuffer buffer)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _ = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            this._logger = logger.ForContext<TradingTeam>();
            this._buffer = buffer;
            this._team = new List<IBroker>();
            this._brokerSynchronizer = new SemaphoreSlim(1, 1);

            // Timer
            this._jobBrokerCheck = new Job(new TimeSpan(0, 0, 5), _brokerCheck);
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Checks for new and obsolete Symbols and creates new <c>Broker</c>s or disposes them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _brokerCheck()
        {
            this.LastActive = DateTime.UtcNow;

            if (this._brokerSynchronizer.CurrentCount == 0)
            {
                return;
            }
            await this._brokerSynchronizer.WaitAsync();

            this._logger.Verbose("Checking trading team...");
            try
            {
                var database = new TrapeContext(Program.Services.GetService<DbContextOptions<TrapeContext>>(), Program.Services.GetService<ILogger>());
                var availableSymbols = database.Symbols.Where(s => s.IsTradingActive).Select(s => s.Name).ToList();

                if (!availableSymbols.Any())
                {
                    this._logger.Error("No symbols active for trading found");
                    return;
                }
                else
                {
                    this._logger.Debug($"Found {availableSymbols.Count()} symbols to trade");
                }

                var obsoleteBrokers = this._team.Where(t => !availableSymbols.Contains(t.Symbol));

                this._logger.Verbose($"Found {obsoleteBrokers.Count()} obsolete brokers");

                // Remove obsolete brokers
                foreach (var obsoleteBroker in obsoleteBrokers)
                {
                    this._logger.Information($"{obsoleteBroker.Symbol}: Removing Broker from the trading team");

                    this._team.Remove(obsoleteBroker);
                    await obsoleteBroker.Finish().ConfigureAwait(true);

                    if (obsoleteBroker is Broker)
                    {
                        var checker = Program.Services.GetService(typeof(IChecker)) as IChecker;
                        checker.Add(obsoleteBroker as Broker);
                    }

                    obsoleteBroker.Dispose();
                }

                var tradedSymbols = this._team.Select(t => t.Symbol);
                var missingSymbols = this._buffer.GetSymbols().Where(b => !tradedSymbols.Contains(b));

                this._logger.Verbose($"Found {missingSymbols.Count()} missing brokers");

                // Add new brokers
                foreach (var missingSymbol in missingSymbols)
                {
                    var broker = Program.Services.GetService(typeof(IBroker)) as IBroker;
                    var checker = Program.Services.GetService(typeof(IChecker)) as IChecker;

                    this._logger.Information($"{missingSymbol}: Adding Broker to the trading team.");

                    this._team.Add(broker);

                    if (broker is Broker)
                    {
                        checker.Add(broker as Broker);
                    }

                    broker.Start(missingSymbol);
                }

                this._logger.Verbose($"Trading Team checked: {this._team.Count()} Broker online");
            }
            finally
            {
                this._brokerSynchronizer.Release();
            }
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            this._logger.Debug("Starting trading team");

            // Call once manually to set up the traders
            _brokerCheck();

            // Start timer for regular checks
            this._jobBrokerCheck.Start();

            var checker = Program.Services.GetService(typeof(IChecker)) as IChecker;
            checker.Add(this);

            this._logger.Debug("Trading team started");
        }

        /// <summary>
        /// Finish
        /// </summary>
        /// <returns></returns>
        public async Task Finish()
        {
            this._logger.Debug("Stopping trading team");

            var checker = Program.Services.GetService(typeof(IChecker)) as IChecker;
            checker.Remove(this);

            this._jobBrokerCheck.Terminate();

            foreach (var trader in this._team)
            {
                await trader.Finish().ConfigureAwait(true);
            }

            this._logger.Debug("Trading team stopped");
        }

        #endregion

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
                foreach (var trader in this._team)
                {
                    trader.Dispose();
                }
                this._jobBrokerCheck.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
