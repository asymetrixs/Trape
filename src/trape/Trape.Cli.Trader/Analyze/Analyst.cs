using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze.Models;
using trape.cli.trader.Cache;
using trape.cli.trader.Team;
using trape.datalayer;
using trape.datalayer.Models;
using trape.jobs;
using Action = trape.datalayer.Enums.Action;
using OrderSide = trape.datalayer.Enums.OrderSide;

namespace trape.cli.trader.Analyze
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
            _cancellationTokenSource = new System.Threading.CancellationTokenSource();
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

            _lastAnalysis.PrepareForUpdate();

            // Get stats
            var stat3s = _buffer.Stats3sFor(Symbol);
            var stat15s = _buffer.Stats15sFor(Symbol);
            var stat2m = _buffer.Stats2mFor(Symbol);
            var stat10m = _buffer.Stats10mFor(Symbol);
            var stat2h = _buffer.Stats2hFor(Symbol);

            // Check that data is valid
            if (stat3s == null
                || stat15s == null
                || stat2m == null
                || stat10m == null)
            {
                _logger.Verbose($"Skipped {Symbol} due to old or incomplete data: 3s:{(stat3s == null)} " +
                    $"15s:{(stat15s == null)} " +
                    $"2m:{(stat2m == null)} " +
                    $"10m:{(stat10m == null)}" +
                    $"2h:{(stat2h == null)}");

                _buffer.UpdateRecommendation(new Recommendation() { Symbol = Symbol, Action = Action.Hold });
                return;
            }

            // Use regular approach
            // Get current symbol price
            var currentPrice = new Point(time: default, price: _buffer.GetBidPrice(Symbol), slope: 0);

            if (currentPrice.Value < 0)
            {
                _logger.Verbose($"Skipped {Symbol} due to old or incomplete data: {currentPrice.Value:0.00}");

                _buffer.UpdateRecommendation(new Recommendation() { Symbol = Symbol, Action = Action.Hold });
                return;
            }

            var movingAverage1h = new Point(price: stat10m.MovingAverage1h, slope: stat10m.Slope1h, slopeBase: TimeSpan.FromHours(1));
            var movingAverage3h = new Point(price: stat10m.MovingAverage3h, slope: stat10m.Slope3h, slopeBase: TimeSpan.FromHours(3));
            var panicLimit = movingAverage3h * 0.9975M;
            var movav1hInterceptingPrice = currentPrice.WillInterceptWith(movingAverage1h);
            var priceInterceptingMovAv1h = movingAverage1h.WillInterceptWith(currentPrice.Value, slope: stat2m.Slope10m, slopeBase: 10 * 60);

            if (movav1hInterceptingPrice != null)
            {
                _logger.Verbose($"{Symbol}: movav1hInterceptingPrice: {movav1hInterceptingPrice.Value:0.00} {movav1hInterceptingPrice.Time.TotalMinutes:#0.00}m");
            }
            if (priceInterceptingMovAv1h != null)
            {
                _logger.Verbose($"{Symbol}: priceInterceptingMovAv1h: {priceInterceptingMovAv1h.Value:0.00} {priceInterceptingMovAv1h.Time.TotalMinutes:#0.00}m");
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
                && stat3s.Slope30s < -currentPrice.Value.XPartOf(15)
                && stat15s.Slope45s < -currentPrice.Value.XPartOf(12)
                && stat15s.Slope1m < -currentPrice.Value.XPartOf(10)
                && stat15s.Slope2m < -currentPrice.Value.XPartOf(5)
                && stat15s.Slope3m < -currentPrice.Value.XPartOf(1)
                // Define threshhold from when on panic mode is active
                && panicLimit < movingAverage3h
                // Price has to drop for more than 10 seconds
                && lastFallingPrice != null
                    && lastFallingPrice.Since < DateTime.UtcNow.AddSeconds(-10)
                )
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
                    && stat3s.Slope30s > currentPrice.Value.XPartOf(9)
                    && stat15s.Slope1m > 0)
            {
                decimal stockQuantity = 0;
                // Check what is in stock
                using (AsyncScopedLifestyle.BeginScope(Program.Container))
                {
                    var database = Program.Container.GetService<TrapeContext>();
                    try
                    {
                        var recordedStockQuantity = database.PlacedOrders
                                                .Where(p => p.Side == OrderSide.Buy
                                                    && p.Symbol == Symbol
                                                    && p.QuantityFilled > 0)
                                                .SelectMany(f => f.Fills.Where(f => f.Quantity > f.ConsumedQuantity))
                                                .Sum(f => f.Quantity - f.ConsumedQuantity);

                        var binanceBalance = await _accountant.GetBalance(Asset).ConfigureAwait(true);
                        decimal actualStockQuantity = binanceBalance?.Free ?? 0;

                        stockQuantity = Math.Max(recordedStockQuantity, actualStockQuantity);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex.Message, ex);
                    }
                }

                var usdt = await _accountant.GetBalance("USDT").ConfigureAwait(false);

                // Calculate value
                var stockValue = stockQuantity * currentPrice.Value;
                var totalValue = stockValue + usdt?.Free;

                // Check if more than 50% of assets are USDT, only then jumpbuy
                if (totalValue.HasValue && totalValue.Value * 0.5M < usdt?.Free)
                {
                    path.Append("jump");
                    // If Slope1h is negative, then only join the jumping trend if the current price
                    // is higher than the value of the Slope1h in 15 minutes
                    if (stat10m.Slope1h < -currentPrice.Value.XPartOf(10))
                    {
                        path.Append("a");
                        // Only jump if price goes higher than intercept with Slope1h in 15 minutes
                        var intercept15min = movingAverage1h.WillInterceptWith(currentPrice);
                        // 15 minutes
                        if (intercept15min?.Time < TimeSpan.FromMinutes(15))
                        {
                            _logger.Verbose("[jump]");
                            action = Action.JumpBuy;
                            _lastAnalysis.JumpDetected();
                            path.Append("|b");
                        }
                    }
                    // Slope1h is (almost) positive, always jump
                    else
                    {
                        path.Append("|b");
                        _logger.Verbose("[jump]");
                        action = Action.JumpBuy;
                        _lastAnalysis.JumpDetected();
                    }
                }
            }
            else if (currentPrice > movingAverage1h)
            {
                if (movingAverage1h.IsClose(movingAverage3h) && stat2m.Slope5m < 0)
                {
                    action = Action.StrongSell;
                    path.Append("|strongsell");
                }
                else if (!movingAverage1h.IsClose(movingAverage3h) && stat2m.Slope5m < 0)
                {
                    action = Action.Sell;
                    path.Append("|sell");
                }
            }
            else if (currentPrice < movingAverage1h)
            {
                if (movingAverage1h.IsClose(movingAverage3h) && stat2m.Slope5m > 0)
                {
                    action = Action.StrongBuy;
                    path.Append("|strongbuy");
                }
                else if (!movingAverage1h.IsClose(movingAverage3h) && stat2m.Slope5m > 0)
                {
                    action = Action.Buy;
                    path.Append("|buy");
                }
            }

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
            // If Panic mode ended and action is not buy but trend is strongly upwards
            else if (_lastAnalysis.LastPanicModeEnded.AddMinutes(7) > DateTime.UtcNow
                && action != Action.Buy
                && stat3s.Slope5s > currentPrice.Value.XPartOf(10)
                && stat3s.Slope10s > currentPrice.Value.XPartOf(10)
                && stat3s.Slope15s > currentPrice.Value.XPartOf(7.5M)
                && stat3s.Slope30s > 0
                && stat3s.Slope30s > 0
                && stat10m.Slope3h > -currentPrice.Value.XPartOf(64.8M)
                && currentPrice < movingAverage1h)
            {
                _logger.Verbose($"{Symbol}: Panic mode ended more than 7 minutes ago and trend is strongly upwards, buy.");
                action = Action.Buy;
                path.Append("_|panicend");
            }


            // If strong sell happened or slope is too negative, do not buy immediately
            if ((_lastAnalysis.GetLastDateOf(Action.StrongSell).AddMinutes(2) > DateTime.UtcNow || stat10m.Slope30m < -currentPrice.Value.XPartOf(20))
                && (action == Action.Buy || action == Action.JumpBuy || action == Action.StrongBuy))
            {
                _logger.Verbose($"{Symbol}: Last strong sell was less than 1 minutes ago, don't buy.");
                action = Action.Hold;
                path.Append("_|wait");
            }

            Point raceStartingPrice;
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    // Check if price has gained a lot over the last 30 minutes
                    // Get Price from 30 minutes ago
                    raceStartingPrice = new Point(time: TimeSpan.FromMinutes(-1),
                                                    price: await database.GetLowestPrice(Symbol, DateTime.UtcNow.AddMinutes(-30), _cancellationTokenSource.Token).ConfigureAwait(false),
                                                    slope: 0);
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                    raceStartingPrice = new Point();
                }
            }

            // Advise to sell on <see cref="TakeProfitLimit"/> % gain
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

            // Buy after PanicSell
            if (_lastAnalysis.PanicSellHasEnded
                && (action != Action.StrongSell && action != Action.PanicSell)
                && currentPrice < movingAverage1h
                && _lastAnalysis.BuyAfterPanicSell())
            {
                action = Action.Buy;
                path.Append("_|panicbuy");
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
                Slope5m = stat2m.Slope5m,
                Slope7m = stat2m.Slope7m,
                Slope10m = stat2m.Slope10m,
                Slope15m = stat2m.Slope15m,
                Slope30m = stat10m.Slope30m,
                Slope1h = stat10m.Slope1h,
                Slope2h = stat10m.Slope2h,
                Slope3h = stat10m.Slope3h,
                Slope6h = stat2h.Slope6h,
                Slope12h = stat2h.Slope12h,
                Slope18h = stat2h.Slope18h,
                Slope1d = stat2h.Slope1d,
                MovingAverage5s = stat3s.MovingAverage5s,
                MovingAverage10s = stat3s.MovingAverage10s,
                MovingAverage15s = stat3s.MovingAverage15s,
                MovingAverage30s = stat3s.MovingAverage30s,
                MovingAverage45s = stat15s.MovingAverage45s,
                MovingAverage1m = stat15s.MovingAverage1m,
                MovingAverage2m = stat15s.MovingAverage2m,
                MovingAverage3m = stat15s.MovingAverage3m,
                MovingAverage5m = stat2m.MovingAverage5m,
                MovingAverage7m = stat2m.MovingAverage7m,
                MovingAverage10m = stat2m.MovingAverage10m,
                MovingAverage15m = stat2m.MovingAverage15m,
                MovingAverage30m = stat10m.MovingAverage30m,
                MovingAverage1h = stat10m.MovingAverage1h,
                MovingAverage2h = stat10m.MovingAverage2h,
                MovingAverage3h = stat10m.MovingAverage3h,
                MovingAverage6h = stat2h.MovingAverage6h,
                MovingAverage12h = stat2h.MovingAverage12h,
                MovingAverage18h = stat2h.MovingAverage18h,
                MovingAverage1d = stat2h.MovingAverage1d
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

            LogTrend(newRecommendation, stat10m, stat2h);
        }

        /// <summary>
        /// Returns a string representing current trend data
        /// </summary>
        /// <param name="recommendation">The recommendation that was calculated</param>
        /// <param name="stat10m">10 minutes stats</param>
        /// <param name="stat2Hours">2 hours stats</param>
        /// <returns>String that returns current trends</returns>
        private void LogTrend(Recommendation recommendation, Stats10m stat10m, Stats2h stat2Hours)
        {
            // Announce the trend every second for reduced log spamming
            if (DateTime.UtcNow.Second != _logTrendLimiter)
            {
                _logTrendLimiter = DateTime.UtcNow.Second;

                var reco = recommendation.Action == datalayer.Enums.Action.Buy ? "Buy :" : recommendation.Action.ToString();

                _logger.Verbose($"{recommendation.Symbol}: {reco} | S1h: {stat10m.Slope1h:0.0000} | S2h: {stat10m.Slope2h:0.0000} | MA1h: {stat10m.MovingAverage1h:0.0000} | MA2h: {stat10m.MovingAverage2h:0.0000} | MA6h: {stat2Hours.MovingAverage6h:0.0000}");
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
            Asset = symbol.Replace("USDT", string.Empty);

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

            await Task.CompletedTask;
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
