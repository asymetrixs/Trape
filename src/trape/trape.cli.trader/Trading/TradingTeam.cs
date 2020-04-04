using Serilog;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using trape.cli.trader.Cache;

namespace trape.cli.trader.Trading
{
    public class TradingTeam : ITradingTeam
    {
        #region Fields

        private bool _disposed;

        private readonly ILogger _logger;

        private List<ITrader> _team;

        private readonly IBuffer _buffer;

        private System.Timers.Timer _timerSymbolCheck;

        #endregion

        #region Constructor 

        public TradingTeam(ILogger logger, IBuffer buffer)
        {
            if (null == logger || null == buffer)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._buffer = buffer;
            this._team = new List<ITrader>();
            this._timerSymbolCheck = new System.Timers.Timer()
            {
                Interval = new TimeSpan(0, 0, 5).TotalMilliseconds,
                AutoReset = true
            };
            this._timerSymbolCheck.Elapsed += _timerSymbolCheck_Elapsed;
        }

        #endregion

        #region Timer Elapsed

        private async void _timerSymbolCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._logger.Verbose("Checking trading team");

            var availableSymbols = this._buffer.GetSymbols();

            var obsoleteTraders = this._team.Where(t => !availableSymbols.Contains(t.Symbol));

            // Remove obsolete traders
            foreach (var obsoleteTrader in obsoleteTraders)
            {
                this._logger.Information($"Removing {obsoleteTrader.Symbol} from the trading team");

                this._team.Remove(obsoleteTrader);
                await obsoleteTrader.Stop().ConfigureAwait(true);
                obsoleteTrader.Dispose();
            }

            var tradedSymbols = this._team.Select(t => t.Symbol);
            var missingSymbols = this._buffer.GetSymbols().Where(b => !tradedSymbols.Contains(b));

            // Add new traders
            foreach (var missingSymbol in missingSymbols)
            {
                var trader = Program.Services.GetService(typeof(ITrader)) as ITrader;

                this._logger.Information($"Adding {missingSymbol} to the trading team.");

                this._team.Add(trader);

                trader.Start(missingSymbol);
            }
        }

        #endregion

        #region Start / Stop

        public void Start()
        {
            this._logger.Information("Starting trading team");

            // Call once manually to set up the traders
            _timerSymbolCheck_Elapsed(null, null);

            // Start timer for regular checks
            this._timerSymbolCheck.Start();

            this._logger.Information("Trading team started");
        }

        public async Task Stop()
        {
            this._logger.Information("Stopping trading team");

            this._timerSymbolCheck.Stop();

            foreach (var trader in this._team)
            {
                await trader.Stop().ConfigureAwait(true);
            }

            this._logger.Information("Trading team stopped");
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
