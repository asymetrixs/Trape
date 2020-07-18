using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.Trading;
using trape.datalayer;
using trape.jobs;

namespace trape.cli.trader.Team
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

            this._buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            this._logger = logger.ForContext<TradingTeam>();
            this._team = new List<IStartable>();

            // Timer
            this._jobMemberCheck = new Job(new TimeSpan(0, 0, 5), _memberCheck);
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Checks for new and obsolete Symbols and creates new <c>Broker</c>s or disposes them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _memberCheck()
        {
            this.LastActive = DateTime.UtcNow;

            this._logger.Verbose("Checking trading team...");

            // Get available symbols
            var availableSymbols = new List<string>();
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    availableSymbols = database.Symbols.Where(s => s.IsTradingActive).Select(s => s.Name).ToList();
                }
                catch (Exception e)
                {
                    this._logger.Error(e, e.Message);
                }
            }

            // Check if any
            if (!availableSymbols.Any())
            {
                this._logger.Error("No symbols active for trading found");
                return;
            }
            else
            {
                this._logger.Debug($"Found {availableSymbols.Count()} symbols to trade");
            }

            // Get obsolete symbols
            var obsoleteSymbols = this._team.Where(t => !availableSymbols.Contains(t.Symbol));
            this._logger.Verbose($"Found {obsoleteSymbols.Count()} obsolete members");

            // Remove obsolete brokers
            foreach (var obsoleteSymbol in obsoleteSymbols)
            {
                this._logger.Information($"{obsoleteSymbol.Symbol}: Removing member from the trading team");

                // Terminate all
                var obsoleteMember = this._team.Where(t => t.Symbol == obsoleteSymbol.Symbol).ToArray();
                for (int i = 0; i < obsoleteMember.Count(); i++)
                {
                    await obsoleteMember[i].Terminate().ConfigureAwait(true);
                    this._team.Remove(obsoleteMember[i]);
                    obsoleteMember[i].Dispose();
                    obsoleteMember[i] = null;
                }
            }

            // Get missing symbols
            var tradedSymbols = this._team.Select(t => t.Symbol);
            var missingSymbols = this._buffer.GetSymbols().Where(b => !tradedSymbols.Contains(b));

            this._logger.Verbose($"Found missing/online/total {missingSymbols.Count()}/{tradedSymbols.Count()}/{availableSymbols.Count} due to incomplete stats.");

            // Add new brokers
            foreach (var missingSymbol in missingSymbols)
            {
                // Instantiate pair of broker and analyst
                var broker = Program.Container.GetInstance<IBroker>();
                var analyst = Program.Container.GetInstance<IAnalyst>();

                this._logger.Information($"{missingSymbol}: Adding Broker to the trading team.");
                this._team.Add(broker);
                broker.Start(missingSymbol);

                this._logger.Information($"{missingSymbol}: Adding Analyst to the trading team.");
                this._team.Add(analyst);
                analyst.Start(missingSymbol);

            }

            this._logger.Verbose($"Trading Team checked: {this._team.Count()} Member online");
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
            this._memberCheck();

            // Start timer for regular checks
            this._jobMemberCheck.Start();

            this._logger.Debug("Trading team started");
        }

        /// <summary>
        /// Terminate
        /// </summary>
        /// <returns></returns>
        public async Task Terminate()
        {
            this._logger.Debug("Stopping trading team");

            this._jobMemberCheck.Terminate();

            foreach (var member in this._team)
            {
                await member.Terminate().ConfigureAwait(true);
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
                foreach (var member in this._team)
                {
                    member.Dispose();
                }
                this._jobMemberCheck.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
