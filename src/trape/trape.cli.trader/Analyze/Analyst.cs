using Microsoft.EntityFrameworkCore;
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

            _ = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _ = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            #endregion

            this._logger = logger.ForContext<Analyst>();
            this._buffer = buffer;
            this._accountant = accountant;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._disposed = false;
            this._logTrendLimiter = 61;

            // Set up timer that makes decisions, every second
            this._jobRecommendationMaker = new Job(new TimeSpan(0, 0, 0, 0, 250), _recommending, this._cancellationTokenSource.Token);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; private set; }

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
        private async void _recommending()
        {
            this.LastActive = DateTime.UtcNow;

            if (string.IsNullOrEmpty(this.Symbol))
            {
                this._logger.Warning($"{nameof(this.Symbol)} is empty, cancelling.");
                return;
            }

            #endregion

            this._lastAnalysis.PrepareForUpdate();

            // Get stats
            var stat3s = this._buffer.Stats3sFor(this.Symbol);
            var stat15s = this._buffer.Stats15sFor(this.Symbol);
            var stat2m = this._buffer.Stats2mFor(this.Symbol);
            var stat10m = this._buffer.Stats10mFor(this.Symbol);
            var stat2h = this._buffer.Stats2hFor(this.Symbol);

            // Check that data is valid
            if (stat3s == null || !stat3s.IsValid()
                || stat15s == null || !stat15s.IsValid()
                || stat2m == null || !stat2m.IsValid()
                || stat10m == null || !stat10m.IsValid())
            {
                this._logger.Warning($"Skipped {this.Symbol} due to old or incomplete data: 3s:{(stat3s == null ? false : stat3s?.IsValid())} " +
                    $"15s:{(stat15s == null ? false : stat15s?.IsValid())} " +
                    $"2m:{(stat2m == null ? false : stat2m?.IsValid())} " +
                    $"10m:{(stat10m == null ? false : stat10m?.IsValid())}");

                this._buffer.UpdateRecommendation(new Recommendation() { Symbol = this.Symbol, Action = Action.Hold });
                return;
            }

            // Use regular approach
            // Get current symbol price
            var currentPrice = new Point(time: default, price: this._buffer.GetBidPrice(this.Symbol), slope: 0);
            var movingAverage1h = new Point(price: stat10m.MovingAverage1h, slope: stat10m.Slope1h, slopeBase: TimeSpan.FromHours(1));
            var movingAverage3h = new Point(price: stat10m.MovingAverage3h, slope: stat10m.Slope3h, slopeBase: TimeSpan.FromHours(3));
            var panicLimit = movingAverage3h * 0.9975M;
            var movav1hInterceptingPrice = currentPrice.WillInterceptWith(movingAverage1h);
            var priceInterceptingMovAv1h = movingAverage1h.WillInterceptWith(currentPrice.Price, slope: stat2m.Slope10m, slopeBase: 10 * 60);

            if (movav1hInterceptingPrice != null)
            {
                this._logger.Verbose($"{this.Symbol}: movav1hInterceptingPrice: {movav1hInterceptingPrice.Price:0.00} {movav1hInterceptingPrice.Time.TotalMinutes:#0.00}m");
            }
            if (priceInterceptingMovAv1h != null)
            {
                this._logger.Verbose($"{this.Symbol}: priceInterceptingMovAv1h: {priceInterceptingMovAv1h.Price:0.00} {priceInterceptingMovAv1h.Time.TotalMinutes:#0.00}m");
            }

            // Make the decision
            var action = Action.Hold;
            var lastFallingPrice = this._buffer.GetLastFallingPrice(this.Symbol);
            if (lastFallingPrice != null)
            {
                this._logger.Verbose($"{this.Symbol}: Last Falling Price Original: {lastFallingPrice.OriginalPrice:0.00} | Since: {lastFallingPrice.Since.ToShortTimeString()}");
            }

            var path = new StringBuilder();

            // Panic
            if (stat3s.Slope5s < -8
                && stat3s.Slope10s < -5
                && stat3s.Slope15s < -6
                && stat3s.Slope30s < -15
                && stat15s.Slope45s < -12
                && stat15s.Slope1m < -10
                && stat15s.Slope2m < -5
                && stat15s.Slope3m < -1
                // Define threshhold from when on panic mode is active
                && panicLimit < movingAverage3h
                // Price has to drop for more than 10 seconds
                && lastFallingPrice != null
                    && lastFallingPrice.Since < DateTime.UtcNow.AddSeconds(-10)
                )
            {
                // Panic sell
                action = Action.PanicSell;

                this._lastAnalysis.PanicDetected();

                this._logger.Warning($"{this.Symbol}: {currentPrice.Price:0.00} - Panic Mode");
                path.Append("|panic");
            }
            // Jump increase
            else if (stat3s.Slope5s > 15
                    && stat3s.Slope10s > 10
                    && stat3s.Slope15s > 15
                    && stat3s.Slope30s > 9
                    && stat15s.Slope1m > 0)
            {
                decimal? stockQuantity = null;
                // Check what is in stock
                using (AsyncScopedLifestyle.BeginScope(Program.Container))
                {
                    var database = Program.Container.GetService<TrapeContext>();
                    try
                    {
                        // TODO: Think about querying Binance this._accountant.GetBalance("BTC")
                        stockQuantity = database.PlacedOrders
                                                .Where(p => p.Side == OrderSide.Buy
                                                    && p.Symbol == this.Symbol
                                                    && p.ExecutedQuantity > 0)
                                                .SelectMany(f => f.Fills.Where(f => f.Quantity > f.ConsumedQuantity))
                                                .Sum(f => f.Quantity - f.ConsumedQuantity);
                    }
                    catch (Exception ex)
                    {
                        this._logger.Error(ex.Message, ex);
                    }
                }

                var usdt = await this._accountant.GetBalance("USDT").ConfigureAwait(false);

                // Calculate value
                var stockValue = stockQuantity * currentPrice.Price;
                var totalValue = stockValue + usdt?.Free;
                // Check if more than 60% of assets are USDT, only then jumpbuy
                if (totalValue.HasValue && totalValue.Value * 0.6M < usdt?.Free)
                {
                    path.Append("jump");
                    // If Slope1h is negative, then only join the jumping trend if the current price
                    // is higher than the value of the Slope1h in 15 minutes
                    if (stat10m.Slope1h < -10)
                    {
                        path.Append("a");
                        // Only jump if price goes higher than intercept with Slope1h in 15 minutes
                        var intercept15min = movingAverage1h.WillInterceptWith(currentPrice);
                        // 15 minutes
                        if (intercept15min?.Time < TimeSpan.FromMinutes(15))
                        {
                            this._logger.Verbose("[jump]");
                            action = Action.JumpBuy;
                            this._lastAnalysis.JumpDetected();
                            path.Append("|b");
                        }
                    }
                    // Slope1h is (almost) positive, always jump
                    else
                    {
                        path.Append("|b");
                        this._logger.Verbose("[jump]");
                        action = Action.JumpBuy;
                        this._lastAnalysis.JumpDetected();
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

            // If a race is ongoing or after it has stopped wait for 9 minutes for market to cool down
            // before another buy is made
            if (this._lastAnalysis.LastRaceEnded.AddMinutes(9) > DateTime.UtcNow
                && (action == Action.Buy || action == Action.JumpBuy || action == Action.StrongBuy))
            {
                this._logger.Verbose($"{this.Symbol}: Race ended less than 9 minutes ago, don't buy.");
                action = Action.Hold;
                path.Append("_|race");
            }


            // If Panic mode ended, wait for 5 minutes before start buying again, except if jump
            if (this._lastAnalysis.LastPanicModeEnded.AddMinutes(5) > DateTime.UtcNow)
            {
                path.Append("_panicend");
                if (action == Action.Buy || action == Action.StrongBuy)
                {
                    this._logger.Verbose($"{this.Symbol}: Panic mode ended less than 5 minutes ago, don't buy.");
                    action = Action.Hold;
                    path.Append("|a");
                }
            }
            // If Panic mode ended and action is not buy but trend is strongly upwards
            else if (this._lastAnalysis.LastPanicModeEnded.AddMinutes(7) > DateTime.UtcNow
                && action != Action.Buy
                && stat3s.Slope5s > 10 && stat3s.Slope10s > 10 && stat3s.Slope15s > 7.5M && stat3s.Slope30s > 0 && stat3s.Slope30s > 0 && stat10m.Slope3h > -64.8M
                && currentPrice < movingAverage1h)
            {
                this._logger.Verbose($"{this.Symbol}: Panic mode ended more than 7 minutes ago and trend is strongly upwards, buy.");
                action = Action.Buy;
                path.Append("_|panicend");
            }


            // If strong sell happened or slope is too negative, do not buy immediately
            if ((this._lastAnalysis.GetLastDateOf(Action.StrongSell).AddMinutes(2) > DateTime.UtcNow || stat10m.Slope30m < -20M)
                && (action == Action.Buy || action == Action.JumpBuy || action == Action.StrongBuy))
            {
                this._logger.Verbose($"{this.Symbol}: Last strong sell was less than 1 minutes ago, don't buy.");
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
                                                    price: await database.GetLowestPrice(this.Symbol, DateTime.UtcNow.AddMinutes(-30), this._cancellationTokenSource.Token).ConfigureAwait(false),
                                                    slope: 0);
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                    raceStartingPrice = new Point();
                }
            }

            /// Advise to sell on <see cref="TakeProfitLimit"/> % gain
            if (raceStartingPrice.Price != default && raceStartingPrice < (currentPrice * TakeProfitLimit))
            {
                this._logger.Verbose($"{this.Symbol}: Race detected at {currentPrice.Price:0.00}.");
                this._lastAnalysis.RaceDetected();
                path.Append("_racestart");

                // Check market movement, if a huge sell is detected advice to take profits
                if (stat3s.Slope10s < -10)
                {
                    action = Action.TakeProfitsSell;
                    this._logger.Verbose($"{this.Symbol}: Race ended at {currentPrice.Price:0.00}.");
                    path.Append("|raceend");
                    this._lastAnalysis.RaceEnded();
                }
            }

            // Buy after PanicSell
            if (this._lastAnalysis.PanicSellHasEnded
                && (action != Action.StrongSell && action != Action.PanicSell)
                && currentPrice < movingAverage1h
                && this._lastAnalysis.BuyAfterPanicSell())
            {
                action = Action.Buy;
                path.Append("_|panicbuy");
            }


            // Print strategy changes
            if (this._lastAnalysis.Action != action)
            {
                this._logger.Information($"{this.Symbol}: {currentPrice.Price:0.00} - Switching stategy: {this._lastAnalysis.Action} -> {action}");
                this._lastAnalysis.UpdateAction(action);
            }
            // Print strategy every hour in log
            else if (this._lastAnalysis.Now.Minute == 0 && this._lastAnalysis.Now.Second == 0 && this._lastAnalysis.Now.Millisecond < 100)
            {
                this._logger.Information($"{this.Symbol}: Stategy: {action}");
            }

            this._logger.Debug($"{this.Symbol}: {currentPrice.Price:0.00} Decision - Calculated / Final: {calcAction} / {action} - {path}");

            // Instantiate new recommendation
            var newRecommendation = new Recommendation()
            {
                Action = action,
                Price = currentPrice.Price,
                Symbol = this.Symbol,
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


            var oldRecommendation = this._buffer.GetRecommendation(this.Symbol);
            this._buffer.UpdateRecommendation(newRecommendation);

            this._logger.Verbose($"{this.Symbol}: Recommending: {action}");

            if (oldRecommendation.Action != newRecommendation.Action)
            {
                this._logger.Information($"{this.Symbol}: Recommendation changed: {oldRecommendation.Action} -> {newRecommendation.Action}");
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
                    this._logger.Error(e.Message, e);
                }
            }

            _logTrend(newRecommendation, stat10m, stat2h);
        }

        /// <summary>
        /// Returns a string representing current trend data
        /// </summary>
        /// <param name="recommendation">The recommendation that was calculated</param>
        /// <param name="stat10m">10 minutes stats</param>
        /// <param name="stat2Hours">2 hours stats</param>
        /// <returns>String that returns current trends</returns>
        private void _logTrend(Recommendation recommendation, Stats10m stat10m, Stats2h stat2Hours)
        {
            // Announce the trend every second for reduced log spamming
            if (DateTime.UtcNow.Second != this._logTrendLimiter)
            {
                this._logTrendLimiter = DateTime.UtcNow.Second;

                var reco = recommendation.Action == datalayer.Enums.Action.Buy ? "Buy :" : recommendation.Action.ToString();

                this._logger.Verbose($"{recommendation.Symbol}: {reco} | S1h: {stat10m.Slope1h:0.0000} | S2h: {stat10m.Slope2h:0.0000} | MA1h: {stat10m.MovingAverage1h:0.0000} | MA2h: {stat10m.MovingAverage2h:0.0000} | MA6h: {stat2Hours.MovingAverage6h:0.0000}");
            }
        }

        #region Start / Terminate

        /// <summary>
        /// Starts the <c>Analyst</c> instance
        /// </summary>
        public void Start(string symbol)
        {
            this.Symbol = symbol;

            if (this._jobRecommendationMaker.Enabled)
            {
                this._logger.Warning($"{this.Symbol}: Analyst is already active");
                return;
            }

            this._logger.Information($"{this.Symbol}: Starting Analyst");

            if (this._buffer.GetSymbols().Contains(this.Symbol))
            {
                this.Symbol = symbol;

                using (AsyncScopedLifestyle.BeginScope(Program.Container))
                {
                    var database = Program.Container.GetService<TrapeContext>();
                    try
                    {
                        var decisions = database.GetLastDecisions().Where(d => d.Symbol == this.Symbol);

                        this._lastAnalysis = new Analysis(this.Symbol, decisions);
                    }
                    catch (Exception e)
                    {
                        this._logger.Error(e.Message, e);
                        throw;
                    }
                }

                this._jobRecommendationMaker.Start();

                this._logger.Information($"{this.Symbol}: Analyst started");
            }
            else
            {
                this._logger.Error($"{this.Symbol}: Analyst cannot be started, symbol does not exist");
            }
        }

        /// <summary>
        /// Stops the <c>Analyst</c> instance
        /// </summary>
        public async Task Terminate()
        {
            // Stop recommendation maker
            this._jobRecommendationMaker.Terminate();

            // Terminate possible running tasks
            this._cancellationTokenSource.Cancel();

            this._logger.Information("Analyst stopped");

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
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._cancellationTokenSource.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
