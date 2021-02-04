using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.trader.Account;
using Trape.Cli.trader.Analyze.Models;
using Trape.Cli.trader.Cache;
using Trape.Cli.trader.Team;
using Trape.Datalayer;
using Trape.Datalayer.Models;
using Trape.Jobs;
using Action = Trape.Datalayer.Enums.Action;

namespace Trape.Cli.trader.Analyze
{
    /// <summary>
    /// This class represents an analyst. It's task is to make recommendations on
    /// buying, keeping (wait), or selling assets based on different facts (slope, moving average, etc.)
    /// </summary>
    public class Analyst : IAnalyst, IDisposable, IStartable
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Buffer
        /// </summary>
        private readonly IBuffer _buffer;

        /// <summary>
        /// Accountant
        /// </summary>
        private readonly IAccountant _accountant;

        /// <summary>
        /// Cancellation Token
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Timer when Analyst makes a new decision
        /// </summary>
        private readonly Job _jobRecommendationMaker;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Saves state of last strategy
        /// </summary>
        private Analysis _lastAnalysis;

        /// <summary>
        /// Used to limit trend logging
        /// </summary>
        private int _logTrendLimiter;

        private static readonly TimeSpan _5s = new TimeSpan(0, 0, -5);
        private static readonly TimeSpan _10s = new TimeSpan(0, 0, -10);
        private static readonly TimeSpan _15s = new TimeSpan(0, 0, -15);
        private static readonly TimeSpan _30s = new TimeSpan(0, 0, -30);
        private static readonly TimeSpan _45s = new TimeSpan(0, 0, -45);
        private static readonly TimeSpan _60s = new TimeSpan(0, -1, 0);
        private static readonly TimeSpan _120s = new TimeSpan(0, -2, 0);
        private static readonly TimeSpan _180s = new TimeSpan(0, -3, 0);
        private static readonly TimeSpan _300s = new TimeSpan(0, -5, 0);

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new instance of the <c>Analyst</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="buffer">Buffer</param>
        /// /// <param name="accountant">Accountant</param>
        public Analyst(ILogger logger, IBuffer buffer, IAccountant accountant)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            #endregion

            _logger = logger.ForContext<Analyst>();
            _cancellationTokenSource = new CancellationTokenSource();
            _disposed = false;
            _logTrendLimiter = 61;

            // Set up timer that makes decisions, every second
            _jobRecommendationMaker = new Job(new TimeSpan(0, 0, 0, 0, 250), Recommending, _cancellationTokenSource.Token);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Asset
        /// </summary>
        public string Asset { get; private set; }

        /// <summary>
        /// Take Profit threshold
        /// </summary>
        public const decimal TakeProfitLimit = 0.991M;

        /// <summary>
        /// Last time <c>Broker</c> was active
        /// </summary>
        public DateTime LastActive { get; private set; }

        private int whatever = 0;

        #endregion

        #region Methods

        /// <summary>
        /// Create a new decision for this symbol
        /// </summary>
        private async void Recommending()
        {
            LastActive = DateTime.UtcNow;

            if (string.IsNullOrEmpty(Symbol))
            {
                _logger.Warning($"{nameof(Symbol)} is empty, cancelling.");
                return;
            }

            // Check if buffer is ready
            if (!_buffer.IsReady(Symbol))
            {
                _logger.Verbose($"No data for {nameof(Symbol)}.");
                return;
            }

            _lastAnalysis.PrepareForUpdate();

            // Get stats

            var last5s = _buffer.Slope(Symbol, _5s);
            var last10s = _buffer.Slope(Symbol, _10s);
            var last15s = _buffer.Slope(Symbol, _15s);
            var last30s = _buffer.Slope(Symbol, _30s);


            var last45s = _buffer.Slope(Symbol, _45s);
            var last60s = _buffer.Slope(Symbol, _60s);
            var last120s = _buffer.Slope(Symbol, _120s);
            var last180s = _buffer.Slope(Symbol, _180s);

            if (last5s is null || last10s is null || last15s is null || last30s is null
                || last45s is null || last60s is null || last120s is null || last180s is null)
            {
                _logger.Verbose($"No change for {nameof(Symbol)}.");
                _buffer.UpdateRecommendation(new Recommendation() { Symbol = Symbol, Action = Action.Hold });
                return;
            }

            var stat3s = new Stats3s(last5s.Value, last10s.Value, last15s.Value, last30s.Value);
            var stat15s = new Stats15s(last45s.Value, last60s.Value, last120s.Value, last180s.Value);

            if (++whatever % 50 == 0)
                Console.WriteLine($"{Symbol} a: {last5s.Value,3}\t{last10s.Value,3}\t{last15s.Value,3}\t{last30s.Value,3}\t{last45s.Value,3}\t{last60s.Value,3}\t{last120s.Value,3}\t{last180s.Value,3}");

            // Use regular approach
            // Get current symbol price
            var currentPrice = new Point(time: default, price: _buffer.GetBidPrice(Symbol), slope: 0);

            if (currentPrice.Value < 0)
            {
                _logger.Verbose($"Skipped {Symbol} due to old or incomplete data: {currentPrice.Value:0.00}");

                _buffer.UpdateRecommendation(new Recommendation() { Symbol = Symbol, Action = Action.Hold });
                return;
            }

            // Make the decision
            var action = Action.Hold;
            var lastFallingPrice = _buffer.GetLastFallingPrice(Symbol);
            if (lastFallingPrice != null)
            {
                _logger.Verbose($"{Symbol}: Last Falling Price Original: {lastFallingPrice.OriginalPrice:0.00} | Since: {lastFallingPrice.Since.ToShortTimeString()}");
            }

            var path = new StringBuilder();

            // Panic, threshold is relative to price 
            if (stat3s.Slope5s < -currentPrice.Value.XPartOf(8)
                && stat3s.Slope10s < -currentPrice.Value.XPartOf(5)
                && stat3s.Slope15s < -currentPrice.Value.XPartOf(6)
                && stat3s.Slope30s < -currentPrice.Value.XPartOf(15))
            {
                // Panic sell
                action = Action.PanicSell;

                _lastAnalysis.PanicDetected();

                _logger.Warning($"{Symbol}: {currentPrice.Value:0.00} - Panic Mode");
                path.Append("|panic");
            }
            // Jump increase
            else if (stat3s.Slope5s > currentPrice.Value.XPartOf(15)
                    && stat3s.Slope10s > currentPrice.Value.XPartOf(1)
                    && stat3s.Slope15s > currentPrice.Value.XPartOf(15)
                    && stat3s.Slope30s > currentPrice.Value.XPartOf(9))
            {
                path.Append("jump");
                _logger.Verbose("[jump]");
                action = Action.JumpBuy;
                _lastAnalysis.JumpDetected();
            }

            // Cache action
            var calcAction = action;

            // If a race is ongoing or after it has stopped wait for 5 minutes for market to cool down
            // before another buy is made
            if (_lastAnalysis.LastRaceEnded.AddMinutes(5) > DateTime.UtcNow
                && (action == Action.Buy || action == Action.JumpBuy || action == Action.StrongBuy))
            {
                _logger.Verbose($"{Symbol}: Race ended less than 9 minutes ago, don't buy.");
                action = Action.Hold;
                path.Append("_|race");
            }

            // If Panic mode ended, wait for 5 minutes before start buying again, except if jump
            if (_lastAnalysis.LastPanicModeEnded.AddMinutes(5) > DateTime.UtcNow)
            {
                path.Append("_panicend");
                if (action == Action.Buy || action == Action.StrongBuy)
                {
                    _logger.Verbose($"{Symbol}: Panic mode ended less than 5 minutes ago, don't buy.");
                    action = Action.Hold;
                    path.Append("|a");
                }
            }

            // If strong sell happened or slope is too negative, do not buy immediately
            if ((_lastAnalysis.GetLastDateOf(Action.StrongSell).AddMinutes(2) > DateTime.UtcNow)
                && (action == Action.Buy || action == Action.JumpBuy || action == Action.StrongBuy))
            {
                _logger.Verbose($"{Symbol}: Last strong sell was less than 1 minutes ago, don't buy.");
                action = Action.Hold;
                path.Append("_|wait");
            }

            Point raceStartingPrice;
            try
            {
                // Check if price has gained a lot over the last 30 minutes
                // Get Price from 30 minutes ago
                raceStartingPrice = new Point(time: TimeSpan.FromMinutes(-5),
                                                price: _buffer.GetLowestPrice(Symbol, _300s),
                                                slope: 0);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                raceStartingPrice = new Point();
            }

            /// Advise to sell on <see cref="TakeProfitLimit"/> % gain
            var raceIdentifier = TakeProfitLimit;
            if (currentPrice.Value < 100)
            {
                raceIdentifier = 0.97M;
            }

            if (raceStartingPrice.Value != default && raceStartingPrice < (currentPrice * raceIdentifier))
            {
                _logger.Verbose($"{Symbol}: Race detected at {currentPrice.Value:0.00}.");
                _lastAnalysis.RaceDetected();
                path.Append("_racestart");

                // Check market movement, if a huge sell is detected advice to take profits
                if (stat3s.Slope10s < -currentPrice.Value.XPartOf(10))
                {
                    action = Action.TakeProfitsSell;
                    _logger.Verbose($"{Symbol}: Race ended at {currentPrice.Value:0.00}.");
                    path.Append("|raceend");
                    _lastAnalysis.RaceEnded();
                }
            }

            // Print strategy changes
            if (_lastAnalysis.Action != action)
            {
                _logger.Information($"{Symbol}: {currentPrice.Value:0.00} - Switching stategy: {_lastAnalysis.Action} -> {action}");
                _lastAnalysis.UpdateAction(action);
            }
            // Print strategy every hour in log
            else if (_lastAnalysis.Now.Minute == 0 && _lastAnalysis.Now.Second == 0 && _lastAnalysis.Now.Millisecond < 100)
            {
                _logger.Information($"{Symbol}: Stategy: {action}");
            }

            _logger.Debug($"{Symbol}: {currentPrice.Value:0.00} Decision - Calculated / Final: {calcAction} / {action} - {path}");

            // Instantiate new recommendation
            var newRecommendation = new Recommendation()
            {
                Action = action,
                Price = currentPrice.Value,
                Symbol = Symbol,
                CreatedOn = DateTime.UtcNow,
                Slope5s = stat3s.Slope5s,
                Slope10s = stat3s.Slope10s,
                Slope15s = stat3s.Slope15s,
                Slope30s = stat3s.Slope30s,
                Slope45s = stat15s.Slope45s,
                Slope1m = stat15s.Slope1m,
                Slope2m = stat15s.Slope2m,
                Slope3m = stat15s.Slope3m,
                Slope5m = 0,
                Slope7m = 0,
                Slope10m = 0,
                Slope15m = 0,
                Slope30m = 0,
                Slope1h = 0,
                Slope2h = 0,
                Slope3h = 0,
                Slope6h = 0,
                Slope12h = 0,
                Slope18h = 0,
                Slope1d = 0,
                MovingAverage5s = stat3s.MovingAverage5s,
                MovingAverage10s = stat3s.MovingAverage10s,
                MovingAverage15s = stat3s.MovingAverage15s,
                MovingAverage30s = stat3s.MovingAverage30s,
                MovingAverage45s = stat15s.MovingAverage45s,
                MovingAverage1m = stat15s.MovingAverage1m,
                MovingAverage2m = stat15s.MovingAverage2m,
                MovingAverage3m = stat15s.MovingAverage3m,
                MovingAverage5m = 0,
                MovingAverage7m = 0,
                MovingAverage10m = 0,
                MovingAverage15m = 0,
                MovingAverage30m = 0,
                MovingAverage1h = 0,
                MovingAverage2h = 0,
                MovingAverage3h = 0,
                MovingAverage6h = 0,
                MovingAverage12h = 0,
                MovingAverage18h = 0,
                MovingAverage1d = 0,
            };

            var oldRecommendation = _buffer.GetRecommendation(Symbol);
            _buffer.UpdateRecommendation(newRecommendation);

            _logger.Verbose($"{Symbol}: Recommending: {action}");

            if (oldRecommendation.Action != newRecommendation.Action)
            {
                _logger.Information($"{Symbol}: Recommendation changed: {oldRecommendation.Action} -> {newRecommendation.Action}");
            }

            // Store recommendation in database
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    database.Recommendations.Add(newRecommendation);
                    await database.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                }
            }
        }

        #endregion

        #region Start / Terminate

        /// <summary>
        /// Starts the <c>Analyst</c> instance
        /// </summary>
        public void Start(string symbol)
        {
            _ = symbol ?? throw new ArgumentNullException(paramName: nameof(symbol));

            if (_jobRecommendationMaker.Enabled)
            {
                _logger.Warning($"{symbol}: Analyst is already active");
                return;
            }

            _logger.Information($"{Symbol}: Starting Analyst");

            Symbol = symbol;
            Asset = symbol.Replace("USDT", string.Empty, StringComparison.InvariantCulture);

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    var decisions = (database.GetLastDecisions().Result).Where(d => d.Symbol == Symbol);

                    _lastAnalysis = new Analysis(Symbol, decisions);
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                    throw;
                }
            }

            _jobRecommendationMaker.Start();

            _logger.Information($"{Symbol}: Analyst started");
        }

        /// <summary>
        /// Stops the <c>Analyst</c> instance
        /// </summary>
        public async Task Terminate()
        {
            // Stop recommendation maker
            _jobRecommendationMaker.Terminate();

            // Terminate possible running tasks
            _cancellationTokenSource.Cancel();

            _logger.Information("Analyst stopped");

            await Task.CompletedTask.ConfigureAwait(false);
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
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
