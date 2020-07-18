using Binance.Net.Interfaces;
using Binance.Net.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;
using trape.datalayer;
using trape.datalayer.Models;
using trape.jobs;

namespace trape.cli.trader.Cache
{
    /// <summary>
    /// This class is an implementation of <c>IBuffer</c>
    /// </summary>
    public class Buffer : IBuffer
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Best Ask Price per Symbol
        /// </summary>
        private readonly ConcurrentDictionary<string, BestPrice> _bestAskPrices;

        /// <summary>
        /// Best Bid Price per Symbol
        /// </summary>
        private readonly ConcurrentDictionary<string, BestPrice> _bestBidPrices;

        /// <summary>
        /// Binance Client
        /// </summary>
        private readonly IBinanceClient _binanceClient;

        /// <summary>
        /// Binance Socket Client
        /// </summary>
        private readonly IBinanceSocketClient _binanceSocketClient;

        /// <summary>
        /// Exchange Information
        /// </summary>
        private BinanceExchangeInfo _binanceExchangeInfo;

        /// <summary>
        /// Time when moving average 10m and moving average 30m crossed last
        /// </summary>
        private IEnumerable<LatestMA10mAndMA30mCrossing> _latestMA10mAnd30mCrossing;

        /// <summary>
        /// Time when moving average 10m and moving average 30m crossed last
        /// </summary>
        private IEnumerable<LatestMA30mAndMA1hCrossing> _latestMA30mAnd1hCrossing;

        /// <summary>
        /// Time when moving average 1h and moving average 3h crossed last
        /// </summary>
        private IEnumerable<LatestMA1hAndMA3hCrossing> _latestMA1hAnd3hCrossing;

        /// <summary>
        /// Holds the last time per symbol when the price dropped for the first time
        /// </summary>
        private Dictionary<string, FallingPrice> _fallingPrices;

        /// <summary>
        /// Cache for open orders
        /// </summary>
        private Dictionary<string, OpenOrder> _openOrders;

        /// <summary>
        /// Holds recommendations per symbol that are pushed by <c>Analyst</c> and consumed by <c>Broker</c>
        /// </summary>
        private ConcurrentDictionary<string, Recommendation> _recommendations;

        #region Jobs

        private readonly Job _jobStats3s;

        private readonly Job _jobStats15s;

        private readonly Job _jobStats2m;

        private readonly Job _jobStats10m;

        private readonly Job _jobStats2h;

        private readonly Job _jobForCrossings;

        private readonly Job _jobExchangeInfo;

        #endregion

        #region Stats

        private IEnumerable<Stats3s> _stats3s;

        private IEnumerable<Stats15s> _stats15s;

        private IEnumerable<Stats2m> _stats2m;

        private IEnumerable<Stats10m> _stats10m;

        private IEnumerable<Stats2h> _stats2h;

        #endregion

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Buffer</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="binanceClient">Binance Client</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public Buffer(ILogger logger, IBinanceClient binanceClient, IBinanceSocketClient binanceSocketClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _ = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            _ = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            #endregion

            this._logger = logger.ForContext<Buffer>();
            this._binanceClient = binanceClient;
            this._binanceSocketClient = binanceSocketClient;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._disposed = false;
            this._bestAskPrices = new ConcurrentDictionary<string, BestPrice>();
            this._bestBidPrices = new ConcurrentDictionary<string, BestPrice>();
            this._binanceExchangeInfo = null;
            this._latestMA10mAnd30mCrossing = new List<LatestMA10mAndMA30mCrossing>();
            this._latestMA30mAnd1hCrossing = new List<LatestMA30mAndMA1hCrossing>();
            this._latestMA1hAnd3hCrossing = new List<LatestMA1hAndMA3hCrossing>();
            this._fallingPrices = new Dictionary<string, FallingPrice>();
            this._openOrders = new Dictionary<string, OpenOrder>();
            this._recommendations = new ConcurrentDictionary<string, Recommendation>();

            #region Job setup

            // Set up all jobs that query the Database in different
            // intervals to get recent data
            this._jobStats3s = new Job(new TimeSpan(0, 0, 0, 0, 100), _trend3Seconds, this._cancellationTokenSource.Token);
            this._jobStats15s = new Job(new TimeSpan(0, 0, 0, 0, 250), _trend15Seconds, this._cancellationTokenSource.Token);
            this._jobStats2m = new Job(new TimeSpan(0, 0, 0, 0, 500), _trend2Minutes, this._cancellationTokenSource.Token);
            this._jobStats10m = new Job(new TimeSpan(0, 0, 1), _trend10Minutes, this._cancellationTokenSource.Token);
            this._jobStats2h = new Job(new TimeSpan(0, 0, 3), _trend2Hours, this._cancellationTokenSource.Token);
            this._jobForCrossings = new Job(new TimeSpan(0, 0, 2), _forCrossing, this._cancellationTokenSource.Token);
            this._jobExchangeInfo = new Job(new TimeSpan(0, 1, 0), _exchangeInfo, this._cancellationTokenSource.Token);

            #endregion
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Updates current prices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _forCrossing()
        {
            this._logger.Verbose("Updating Moving Average 10m and Moving Average 30m crossing");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    this._latestMA10mAnd30mCrossing = database.GetLatestMA10mAndMA30mCrossing();
                    this._latestMA30mAnd1hCrossing = database.GetLatestMA30mAndMA1hCrossing();
                    this._latestMA1hAnd3hCrossing = database.GetLatestMA1hAndMA3hCrossing();
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                }
            }

            this._logger.Verbose("Updated Moving Average 10m and Moving Average 30m crossing");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _trend3Seconds()
        {
            this._logger.Verbose("Updating 3 seconds trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    this._stats3s = database.Get3SecondsTrendAsync();
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                }
            }

            // Set falling prices
            foreach (var stat in this._stats3s)
            {
                var currentPrice = this.GetBidPrice(stat.Symbol);

                if (stat.Slope5s < 0)
                {
                    // Add if not in, this is the first time it drops
                    // if record is in, then during the previous run the price
                    // already dropped, otherwise it would have been removed
                    if (!this._fallingPrices.ContainsKey(stat.Symbol))
                    {
                        if (currentPrice != -1)
                        {
                            this._logger.Verbose($"{stat.Symbol}: Falling price added - {currentPrice:0.##} at {DateTime.UtcNow.ToShortTimeString()}");
                            this._fallingPrices.Add(stat.Symbol, new FallingPrice(stat.Symbol, currentPrice, DateTime.UtcNow));
                        }
                    }
                }
                else
                {
                    // Slope 5s is higher than 0, then remove the entry
                    if (this._fallingPrices.ContainsKey(stat.Symbol))
                    {
                        this._fallingPrices.Remove(stat.Symbol, out var value);
                        this._logger.Verbose($"{stat.Symbol}: Falling price removed - {value.OriginalPrice:0.##} < {currentPrice:0.##} at {DateTime.UtcNow.ToShortTimeString()}");
                    }
                }
            }

            this._logger.Verbose("Updated 3 seconds trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _trend15Seconds()
        {
            this._logger.Verbose("Updating 15 seconds trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    this._stats15s = database.Get15SecondsTrendAsync();
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                }
            }

            this._logger.Verbose("Updated 15 seconds trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _trend2Minutes()
        {
            this._logger.Verbose("Updating 2 minutes trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    this._stats2m = database.Get2MinutesTrendAsync();
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                }
            }

            this._logger.Verbose("Updated 2 minutes trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _trend10Minutes()
        {
            this._logger.Verbose("Updating 10 minutes trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    this._stats10m = database.Get10MinutesTrendAsync();
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                }
            }

            this._logger.Verbose("Updated 10 minutes trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _trend2Hours()
        {
            this._logger.Verbose("Updating 2 hours trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    this._stats2h = database.Get2HoursTrendAsync();
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                }
            }

            this._logger.Verbose("Updated 2 hours trend");
        }

        /// <summary>
        /// Updates Exchange Information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _exchangeInfo()
        {
            var result = await this._binanceClient.GetExchangeInfoAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);

            if (result.Success)
            {
                this._binanceExchangeInfo = result.Data;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates a recommendation
        /// </summary>
        /// <param name="recommendation"></param>
        public void UpdateRecommendation(Recommendation recommendation)
        {
            #region Argument checks

            _ = recommendation ?? throw new ArgumentNullException(nameof(recommendation));

            #endregion

            this._recommendations.AddOrUpdate(recommendation.Symbol, recommendation, (key, value) => value = recommendation);
        }

        /// <summary>
        /// Returns a recommendation for a <paramref name="symbol"/>.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public Recommendation GetRecommendation(string symbol)
        {
            if (!this._recommendations.ContainsKey(symbol))
            {
                return new Recommendation() { Action = datalayer.Enums.Action.Hold };
            }

            return this._recommendations[symbol];
        }

        /// <summary>
        /// Stores open orders
        /// </summary>
        /// <param name="openOrder">Open order</param>
        public void AddOpenOrder(OpenOrder openOrder)
        {
            #region Argument checks

            if (openOrder == null)
            {
                return;
            }

            #endregion

            if (this._openOrders.ContainsKey(openOrder.GUID))
            {
                this._openOrders.Remove(openOrder.GUID);
            }

            this._logger.Debug($"Order added {openOrder.GUID}");

            this._openOrders.Add(openOrder.GUID, openOrder);
        }

        /// <summary>
        /// Removes an open order
        /// </summary>
        /// <param name="clientOrderId">Id of open order</param>
        public void RemoveOpenOrder(string clientOrderId)
        {
            this._logger.Debug($"Order removed {clientOrderId}");

            this._openOrders.Remove(clientOrderId);
        }

        /// <summary>
        /// Returns the currently blocked
        /// </summary>
        public decimal GetOpenOrderValue(string symbol)
        {
            // Remove old orders
            foreach (var oo in this._openOrders.Where(o => o.Value.CreatedOn < DateTime.UtcNow.AddSeconds(-10)).Select(o => o.Key))
            {
                this._logger.Debug($"Order cleaned {oo}");
                this._openOrders.Remove(oo);
            }

            return this._openOrders.Where(o => o.Value.Symbol == symbol).Sum(o => o.Value.Quantity);
        }

        /// <summary>
        /// Stats3s
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public Stats3s Stats3sFor(string symbol)
        {
            // Reference current buffer data
            var s3s = this._stats3s;

            // Extract data for current symbol
            return s3s.FirstOrDefault(t => t.Symbol == symbol);
        }

        /// <summary>
        /// Stats15s
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public Stats15s Stats15sFor(string symbol)
        {
            // Reference current buffer dat
            var s15s = this._stats15s;

            // Extract data for current symbol
            return s15s.FirstOrDefault(t => t.Symbol == symbol) ?? new Stats15s();
        }

        /// <summary>
        /// Stats2m
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public Stats2m Stats2mFor(string symbol)
        {
            // Reference current buffer data
            var s2m = this._stats2m;

            // Extract data for current symbol
            return s2m.FirstOrDefault(t => t.Symbol == symbol) ?? new Stats2m();
        }

        /// <summary>
        /// Stats10m
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public Stats10m Stats10mFor(string symbol)
        {
            // Reference current buffer data
            var s10m = this._stats10m;

            // Extract data for current symbol
            return s10m.FirstOrDefault(t => t.Symbol == symbol) ?? new Stats10m();
        }

        /// <summary>
        /// Stats2h
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public Stats2h Stats2hFor(string symbol)
        {
            // Reference current buffer data
            var s2h = this._stats2h;

            // Extract data for current symbol            
            return s2h.FirstOrDefault(t => t.Symbol == symbol) ?? new Stats2h();
        }

        /// <summary>
        /// Returns the available symbols the buffer has data for
        /// </summary>
        /// <returns>List of symbols</returns>
        public IEnumerable<string> GetSymbols()
        {
            // Take symbols that are we have data for
            if (this._stats10m == null)
            {
                return new List<string>();
            }

            return this._stats10m.Select(t => t.Symbol);
        }

        /// <summary>
        /// Returns the latest ask price for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Ask price of the symbol</returns>
        public decimal GetAskPrice(string symbol)
        {
            if (!this._bestAskPrices.ContainsKey(symbol))
            {
                this._logger.Debug($"{symbol}: No asking price available");

                // Get price from Binance
                var result = this._binanceClient.GetPrice(symbol);
                if (result.Success)
                {
                    return result.Data.Price;
                }

                this._logger.Warning($"{symbol}: Could not fetch price from Binance");

                return -1;
            }
            else
            {
                return this._bestAskPrices[symbol].GetAverage();
            }
        }

        /// <summary>
        /// Returns the latest bid price for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Bid price of the symol</returns>
        public decimal GetBidPrice(string symbol)
        {
            if (!this._bestBidPrices.ContainsKey(symbol))
            {
                this._logger.Warning($"{symbol}: No bidding price available");
                return -1;
            }
            else
            {
                return this._bestBidPrices[symbol].GetAverage();
            }
        }

        /// <summary>
        /// Returns exchange information for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Exchange information</returns>
        public BinanceSymbol GetSymbolInfoFor(string symbol)
        {
            if (this._binanceExchangeInfo == null || string.IsNullOrEmpty(symbol))
            {
                return null;
            }

            var symbolInfo = this._binanceExchangeInfo.Symbols.FirstOrDefault(s => s.Name == symbol);

            if (symbolInfo == null || symbolInfo.Status != SymbolStatus.Trading)
            {
                this._logger.Warning($"{symbol}: No exchange info available");
                return null;
            }

            return symbolInfo;
        }

        /// <summary>
        /// Returns the last time Slope 10m and Slope 30m were crossing
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public LatestMA10mAndMA30mCrossing GetLatest10mAnd30mCrossing(string symbol)
        {
            // Save ref
            var latest = this._latestMA10mAnd30mCrossing;
            return latest.FirstOrDefault(s => s.Symbol == symbol);
        }

        /// <summary>
        /// Returns the last time Slope 30m and Slope 1h were crossing
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public LatestMA30mAndMA1hCrossing GetLatest30mAnd1hCrossing(string symbol)
        {
            // Save ref
            var latest = this._latestMA30mAnd1hCrossing;
            return latest.FirstOrDefault(s => s.Symbol == symbol);
        }

        /// <summary>
        /// Returns the last time Slope 10m and Slope 30m were crossing
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public LatestMA1hAndMA3hCrossing GetLatest1hAnd3hCrossing(string symbol)
        {
            //Save ref
            var latest = this._latestMA1hAnd3hCrossing;
            return latest.FirstOrDefault(s => s.Symbol == symbol);
        }

        /// <summary>
        /// Returns the last falling price
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public FallingPrice GetLastFallingPrice(string symbol)
        {
            var fallingPrice = this._fallingPrices.GetValueOrDefault(symbol);
            if (fallingPrice == default)
            {
                return null;
            }

            return fallingPrice;
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Starts a buffer
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            this._logger.Information("Starting Buffer");

            // Initial loading
            this._logger.Debug("Preloading buffer");

            var availableSymbols = new List<string>();
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    this._stats3s = database.Get3SecondsTrendAsync();
                    this._stats15s = database.Get15SecondsTrendAsync();
                    this._stats2m = database.Get2MinutesTrendAsync();
                    this._stats10m = database.Get10MinutesTrendAsync();
                    this._stats2h = database.Get2HoursTrendAsync();
                    this._latestMA10mAnd30mCrossing = database.GetLatestMA10mAndMA30mCrossing();

                    while (!availableSymbols.Any())
                    {
                        availableSymbols = database.Symbols.Where(s => s.IsTradingActive).Select(s => s.Name).AsNoTracking().ToList();

                        if (!availableSymbols.Any())
                        {
                            this._logger.Information("No active symbols. Sleeping...");
                            await Task.Delay(5000).ConfigureAwait(true);
                        }
                    }
                }
                catch (Exception e)
                {
                    this._logger.Error(e.Message, e);
                }
            }

            if(!availableSymbols.Any())
            {
                this._logger.Error("Cannot check subscriptions");
                return;
            }

            this._logger.Debug("Buffer preloaded");

            this._logger.Information($"Symbols to subscribe to are {string.Join(',', availableSymbols)}, starting the subscription process");

            // Tries 30 times to subscribe to the ticker
            var countTillHardExit = 30;
            while (countTillHardExit > 0)
            {
                try
                {
                    // Subscribe to all symbols
                    await this._binanceSocketClient.SubscribeToBookTickerUpdatesAsync(availableSymbols, async (BinanceBookTick bbt) =>
                    {
                        var askPriceAdded = false;
                        var bidPriceAdded = false;

                        // Update ask price
                        while (!askPriceAdded)
                        {
                            if (this._bestAskPrices.ContainsKey(bbt.Symbol))
                            {
                                await this._bestAskPrices[bbt.Symbol].Add(bbt.BestAskPrice).ConfigureAwait(true);
                                askPriceAdded = true;
                            }
                            else
                            {
                                var bestAskPrice = new BestPrice(bbt.Symbol);
                                askPriceAdded = this._bestAskPrices.TryAdd(bbt.Symbol, bestAskPrice);
                                await bestAskPrice.Add(bbt.BestAskPrice).ConfigureAwait(true);
                            }

                            this._logger.Verbose($"{bbt.Symbol}: Book tick update - asking is {bbt.BestAskPrice:0.00}");
                        }

                        // Update bid price
                        while (!bidPriceAdded)
                        {
                            if (this._bestBidPrices.ContainsKey(bbt.Symbol))
                            {
                                await this._bestBidPrices[bbt.Symbol].Add(bbt.BestBidPrice).ConfigureAwait(true);
                                bidPriceAdded = true;
                            }
                            else
                            {
                                var bestBidPrice = new BestPrice(bbt.Symbol);
                                bidPriceAdded = this._bestBidPrices.TryAdd(bbt.Symbol, bestBidPrice);
                                await bestBidPrice.Add(bbt.BestBidPrice).ConfigureAwait(true);
                            }

                            this._logger.Verbose($"{bbt.Symbol}: Book tick update - bidding is {bbt.BestBidPrice:0.00}");
                        }
                    }).ConfigureAwait(true);

                    countTillHardExit = -1;
                }
                catch (Exception e)
                {
                    this._logger.Fatal($"Connecting to Binance failed, retrying, {31 - countTillHardExit}/30");
                    this._logger.Fatal(e.Message, e);

                    countTillHardExit--;

                    // Fails hard and relies on service manager (e.g. systemd) to restart the service
                    if (countTillHardExit == 0)
                    {
                        this._logger.Fatal("Shutting down, relying on systemd to restart");
                        Environment.Exit(1);
                    }

                    await Task.Delay(2000).ConfigureAwait(true);
                }
            }

            this._logger.Debug($"Subscribed to Book Ticker");

            // Starting of timers
            this._jobStats3s.Start();
            this._jobStats15s.Start();
            this._jobStats2m.Start();
            this._jobStats10m.Start();
            this._jobStats2h.Start();
            this._jobForCrossings.Start();

            // Loading exchange information
            this._exchangeInfo();

            this._logger.Information("Buffer started");
        }

        /// <summary>
        /// Stops a buffer
        /// </summary>
        public void Terminate()
        {
            this._logger.Information("Stopping buffer");

            this._cancellationTokenSource.Cancel();

            // Shutdown of timers
            this._jobStats3s.Terminate();
            this._jobStats15s.Terminate();
            this._jobStats2m.Terminate();
            this._jobStats10m.Terminate();
            this._jobStats2h.Terminate();
            this._jobForCrossings.Terminate();

            this._logger.Information("Buffer stopped");
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
                this._jobStats3s.Dispose();
                this._jobStats15s.Dispose();
                this._jobStats2m.Dispose();
                this._jobStats10m.Dispose();
                this._jobStats2h.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
