using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trape.cli.trader.Cache;

namespace trape.cli.trader.Trading
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
        private readonly List<IBroker> _team;

        /// <summary>
        /// Buffer
        /// </summary>
        private readonly IBuffer _buffer;

        /// <summary>
        /// Timer to check for new/obsolete Symbols
        /// </summary>
        private readonly System.Timers.Timer _timerSymbolCheck;

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

            if (logger == null)
            {
                throw new ArgumentNullException(paramName: nameof(logger));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(paramName: nameof(buffer));
            }

            #endregion

            this._logger = logger.ForContext<TradingTeam>();
            this._buffer = buffer;
            this._team = new List<IBroker>();

            // Timer
            this._timerSymbolCheck = new System.Timers.Timer()
            {
                Interval = new TimeSpan(0, 0, 5).TotalMilliseconds,
                AutoReset = true
            };
            this._timerSymbolCheck.Elapsed += _timerSymbolCheck_Elapsed;
        }

        #endregion

        #region Timer Elapsed

        /// <summary>
        /// Checks for new and obsolete Symbols and creates new <c>Broker</c>s or disposes them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timerSymbolCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._logger.Verbose("Checking trading team...");

            var database = Pool.DatabasePool.Get();
            var availableSymbols = database.Symbols.Where(s => s.IsTradingActive).Select(s => s.Name).ToList();
            Pool.DatabasePool.Put(database);

            if (!availableSymbols.Any())
            {
                this._logger.Error("No symbols active for trading found");
                return;
            }
            else
            {
                this._logger.Debug($"Found {availableSymbols.Count} symbols to trade");
            }

            var obsoleteTraders = this._team.Where(t => !availableSymbols.Contains(t.Symbol));

            this._logger.Verbose($"Found {obsoleteTraders.Count()} obsolete brokers");

            // Remove obsolete brokers
            foreach (var obsoleteTrader in obsoleteTraders)
            {
                this._logger.Information($"{obsoleteTrader.Symbol}: Removing Broker from the trading team");

                this._team.Remove(obsoleteTrader);
                await obsoleteTrader.Finish().ConfigureAwait(true);
                obsoleteTrader.Dispose();
            }

            var tradedSymbols = this._team.Select(t => t.Symbol);
            var missingSymbols = this._buffer.GetSymbols().Where(b => !tradedSymbols.Contains(b));

            this._logger.Verbose($"Found {missingSymbols.Count()} missing brokers");

            // Add new brokers
            foreach (var missingSymbol in missingSymbols)
            {
                var trader = Program.Services.GetService(typeof(IBroker)) as IBroker;

                this._logger.Information($"{missingSymbol}: Adding Broker to the trading team.");

                this._team.Add(trader);

                trader.Start(missingSymbol);
            }

            this._logger.Verbose($"Trading Team checked: {this._team.Count()} Broker online");
        }

        #endregion

        #region Start / Stop

        public void Start()
        {
            this._logger.Debug("Starting trading team");

            // Call once manually to set up the traders
            _timerSymbolCheck_Elapsed(null, null);

            // Start timer for regular checks
            this._timerSymbolCheck.Start();

            this._logger.Debug("Trading team started");
        }

        public async Task Finish()
        {
            this._logger.Debug("Stopping trading team");

            this._timerSymbolCheck.Stop();

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
            }

            this._disposed = true;
        }

        #endregion
    }
}
