using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.trader.Analyze;
using Trape.Cli.trader.Cache;
using Trape.Cli.trader.Trading;
using Trape.Jobs;

namespace Trape.Cli.trader.Team
{
    /// <summary>
    /// Starts, stops, and manages the <c>Brokers</c>
    /// </summary>
    public class TradingTeam : ITradingTeam
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
        private readonly List<IStartable> _team;

        /// <summary>
        /// Buffer
        /// </summary>
        private readonly IBuffer _buffer;

        /// <summary>
        /// Job to check for new/obsolete Symbols
        /// </summary>
        private readonly Job _jobMemberCheck;

        /// <summary>
        /// Checks that only one check method is running at a time
        /// </summary>
        private SemaphoreSlim _tradingTeamSetup;

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

            _buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            _logger = logger.ForContext<TradingTeam>();
            _team = new List<IStartable>();
            _tradingTeamSetup = new SemaphoreSlim(1);

            // Timer
            _jobMemberCheck = new Job(new TimeSpan(0, 0, 5), MemberCheck);
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Checks for new and obsolete Symbols and creates new <c>Broker</c>s or disposes them.
        /// </summary>
        private async void MemberCheck()
        {
            if (!await _tradingTeamSetup.WaitAsync(100))
            {
                return;
            }

            LastActive = DateTime.UtcNow;

            _logger.Verbose("Checking trading team...");

            // Get available symbols
            var availableSymbols = _buffer.GetSymbols();

            // Check if any
            if (!availableSymbols.Any())
            {
                _logger.Warning("No symbols active for trading, aborting...");
                _tradingTeamSetup.Release();
                return;
            }
            else
            {
                _logger.Debug($"Found {availableSymbols.Count()} symbols to trade");
            }

            // Get obsolete symbols
            var obsoleteSymbols = _team.Where(t => !availableSymbols.Contains(t.Symbol));
            _logger.Verbose($"Found {obsoleteSymbols.Count()} obsolete members");

            // Remove obsolete brokers
            foreach (var obsoleteSymbol in obsoleteSymbols)
            {
                _logger.Information($"{obsoleteSymbol.Symbol}: Removing member from the trading team");

                // Terminate all
                var obsoleteMember = _team.Where(t => t.Symbol == obsoleteSymbol.Symbol).ToArray();
                for (int i = 0; i < obsoleteMember.Length; i++)
                {
                    await obsoleteMember[i].Terminate().ConfigureAwait(true);
                    _team.Remove(obsoleteMember[i]);
                    obsoleteMember[i].Dispose();
                }
            }

            // Get missing symbols
            var tradedSymbols = _team.Select(t => t.Symbol).Distinct();
            var missingSymbols = _buffer.GetSymbols().Where(b => !tradedSymbols.Contains(b));

            // Add new brokers
            foreach (var missingSymbol in missingSymbols)
            {
                // Instantiate pair of broker and analyst
                var broker = Program.Container.GetInstance<IBroker>();
                var analyst = Program.Container.GetInstance<IAnalyst>();

                _logger.Information($"{missingSymbol}: Adding Broker to the trading team.");
                _team.Add(broker);
                broker.Start(missingSymbol);

                _logger.Information($"{missingSymbol}: Adding Analyst to the trading team.");
                _team.Add(analyst);
                analyst.Start(missingSymbol);
            }

            _logger.Information($"Found missing/online/total {missingSymbols.Count()}/{tradedSymbols.Count()}/{availableSymbols.Count()} due to incomplete stats.");

            _logger.Information($"Trading Team checked: {_team.Count} Member online");

            _tradingTeamSetup.Release();
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            _logger.Debug("Starting trading team");

            // Call once manually to set up the traders
            MemberCheck();

            // Start timer for regular checks
            _jobMemberCheck.Start();

            _logger.Debug("Trading team started");
        }

        /// <summary>
        /// Terminate
        /// </summary>
        /// <returns></returns>
        public async Task Terminate()
        {
            _logger.Debug("Stopping trading team");

            _jobMemberCheck.Terminate();

            Parallel.ForEach(_team, async (team) => await team.Terminate().ConfigureAwait(false));

            _logger.Debug("Trading team stopped");
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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var member in _team)
                {
                    member.Dispose();
                }
                _jobMemberCheck.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
