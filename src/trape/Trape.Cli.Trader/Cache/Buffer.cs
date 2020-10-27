using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.MarketStream;
using CryptoExchange.Net.Sockets;
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
        private readonly IBinanceSocketClient _binanceSocketClients;

        /// <summary>
        /// Holds information about the binance socket subscriptions
        /// </summary>
        private readonly Dictionary<string, UpdateSubscription> _updateSubscriptions;

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
        private readonly Dictionary<string, FallingPrice> _fallingPrices;

        /// <summary>
        /// Cache for open orders
        /// </summary>
        private readonly Dictionary<string, OpenOrder> _openOrders;

        /// <summary>
        /// Holds recommendations per symbol that are pushed by <c>Analyst</c> and consumed by <c>Broker</c>
        /// </summary>
        private readonly ConcurrentDictionary<string, Recommendation> _recommendations;

        #region Jobs

        private readonly Job _jobStats3s;

        private readonly Job _jobStats15s;

        private readonly Job _jobStats2m;

        private readonly Job _jobStats10m;

        private readonly Job _jobStats2h;

        private readonly Job _jobForCrossings;

        private readonly Job _jobExchangeInfo;

        private readonly Job _jobSymbolChecker;

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

            _binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            _binanceSocketClients = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            #endregion

            _logger = logger.ForContext<Buffer>();
            _cancellationTokenSource = new CancellationTokenSource();
            _disposed = false;
            _bestAskPrices = new ConcurrentDictionary<string, BestPrice>();
            _bestBidPrices = new ConcurrentDictionary<string, BestPrice>();
            _binanceExchangeInfo = null;
            _latestMA10mAnd30mCrossing = new List<LatestMA10mAndMA30mCrossing>();
            _latestMA30mAnd1hCrossing = new List<LatestMA30mAndMA1hCrossing>();
            _latestMA1hAnd3hCrossing = new List<LatestMA1hAndMA3hCrossing>();
            _fallingPrices = new Dictionary<string, FallingPrice>();
            _openOrders = new Dictionary<string, OpenOrder>();
            _recommendations = new ConcurrentDictionary<string, Recommendation>();
            _updateSubscriptions = new Dictionary<string, UpdateSubscription>();

            #region Job setup

            // Set up all jobs that query the Database in different
            // intervals to get recent data
            _jobStats3s = new Job(new TimeSpan(0, 0, 0, 0, 100), async () => await Trend3Seconds().ConfigureAwait(true), _cancellationTokenSource.Token);
            _jobStats15s = new Job(new TimeSpan(0, 0, 0, 0, 250), async () => await Trend15Seconds().ConfigureAwait(true), _cancellationTokenSource.Token);
            _jobStats2m = new Job(new TimeSpan(0, 0, 0, 0, 500), async () => await Trend2Minutes().ConfigureAwait(true), _cancellationTokenSource.Token);
            _jobStats10m = new Job(new TimeSpan(0, 0, 1), async () => await Trend10Minutes().ConfigureAwait(true), _cancellationTokenSource.Token);
            _jobStats2h = new Job(new TimeSpan(0, 0, 3), async () => await Trend2Hours().ConfigureAwait(true), _cancellationTokenSource.Token);
            _jobForCrossings = new Job(new TimeSpan(0, 0, 2), async () => await ForCrossing().ConfigureAwait(true), _cancellationTokenSource.Token);
            _jobExchangeInfo = new Job(new TimeSpan(0, 1, 0), ExchangeInfo, _cancellationTokenSource.Token);
            _jobSymbolChecker = new Job(new TimeSpan(0, 0, 10), CheckSubscriptions, _cancellationTokenSource.Token);

            #endregion
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Updates current prices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task ForCrossing()
        {
            _logger.Verbose("Updating Moving Average 10m and Moving Average 30m crossing");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    _latestMA10mAnd30mCrossing = await database.GetLatestMA10mAndMA30mCrossing().ConfigureAwait(true);
                    _latestMA30mAnd1hCrossing = await database.GetLatestMA30mAndMA1hCrossing().ConfigureAwait(true);
                    _latestMA1hAnd3hCrossing = await database.GetLatestMA1hAndMA3hCrossing().ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                }
            }

            _logger.Verbose("Updated Moving Average 10m and Moving Average 30m crossing");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Trend3Seconds()
        {
            _logger.Verbose("Updating 3 seconds trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();

                try
                {
                    _stats3s = await database.Get3SecondsTrendAsync().ConfigureAwait(true);
                }
                catch
                {
                    _stats3s = new List<Stats3s>();
                    _logger.Debug("Failed to update 3 seconds trend");
                    return;
                }
            }

            // Set falling prices
            foreach (var stat in _stats3s)
            {
                var currentPrice = GetBidPrice(stat.Symbol);

                if (stat.Slope5s < 0)
                {
                    // Add if not in, this is the first time it drops
                    // if record is in, then during the previous run the price
                    // already dropped, otherwise it would have been removed
                    if (!_fallingPrices.ContainsKey(stat.Symbol))
                    {
                        if (currentPrice != -1)
                        {
                            _logger.Verbose($"{stat.Symbol}: Falling price added - {currentPrice:0.##} at {DateTime.UtcNow.ToShortTimeString()}");
                            _fallingPrices.Add(stat.Symbol, new FallingPrice(stat.Symbol, currentPrice, DateTime.UtcNow));
                        }
                    }
                }
                else
                {
                    // Slope 5s is higher than 0, then remove the entry
                    if (_fallingPrices.ContainsKey(stat.Symbol))
                    {
                        _fallingPrices.Remove(stat.Symbol, out var value);
                        _logger.Verbose($"{stat.Symbol}: Falling price removed - {value.OriginalPrice:0.##} < {currentPrice:0.##} at {DateTime.UtcNow.ToShortTimeString()}");
                    }
                }
            }

            _logger.Verbose("Updated 3 seconds trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Trend15Seconds()
        {
            _logger.Verbose("Updating 15 seconds trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();

                try
                {
                    _stats15s = await database.Get15SecondsTrendAsync().ConfigureAwait(true);
                }
                catch
                {
                    _stats15s = new List<Stats15s>();
                    _logger.Debug("Failed to update 15 seconds trend");
                    return;
                }
            }

            _logger.Verbose("Updated 15 seconds trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Trend2Minutes()
        {
            _logger.Verbose("Updating 2 minutes trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();

                try
                {
                    _stats2m = await database.Get2MinutesTrendAsync().ConfigureAwait(true);
                }
                catch
                {
                    _stats2m = new List<Stats2m>();
                    _logger.Debug("Failed to update 2 minutes trend");
                    return;
                }
            }

            _logger.Verbose("Updated 2 minutes trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Trend10Minutes()
        {
            _logger.Verbose("Updating 10 minutes trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();

                try
                {
                    _stats10m = await database.Get10MinutesTrendAsync().ConfigureAwait(true);
                }
                catch
                {
                    _stats10m = new List<Stats10m>();
                    _logger.Debug("Failed to update 10 minutes trend");
                    return;
                }
            }

            _logger.Verbose("Updated 10 minutes trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Trend2Hours()
        {
            _logger.Verbose("Updating 2 hours trend");

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();

                try
                {
                    _stats2h = await database.Get2HoursTrendAsync().ConfigureAwait(true);
                }
                catch
                {
                    _stats2h = new List<Stats2h>();
                    _logger.Debug("Failed to update 2 hours trend");
                    return;
                }
            }

            _logger.Verbose("Updated 2 hours trend");
        }

        /// <summary>
        /// Updates Exchange Information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ExchangeInfo()
        {
            var result = await _binanceClient.Spot.System.GetExchangeInfoAsync(_cancellationTokenSource.Token).ConfigureAwait(true);

            if (result.Success)
            {
                _binanceExchangeInfo = result.Data;
            }
        }

        /// <summary>
        /// Checks symbols
        /// </summary>
        /// <returns></returns>
        private async void CheckSubscriptions()
        {
            // Initial loading
            _logger.Debug("Checking subscriptions");

            var availableSymbols = new List<string>();
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    availableSymbols = database.Symbols.Where(s => s.IsTradingActive).Select(s => s.Name).AsNoTracking().ToList();
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                }
            }

            if (!availableSymbols.Any())
            {
                _logger.Error("Cannot check subscriptions");
                return;
            }

            _logger.Debug($"Symbols subscribed to are {string.Join(',', _updateSubscriptions.Keys)}");
            var unsubscribeFrom = _updateSubscriptions.Keys.Where(s => !availableSymbols.Contains(s));
            var subscribeTo = availableSymbols.Where(s => !_updateSubscriptions.ContainsKey(s));

            // Unsubscribe
            if (unsubscribeFrom.Any())
            {
                _logger.Information($"Symbols to unsubscribe from are {string.Join(',', unsubscribeFrom)}");

                foreach (var symbol in unsubscribeFrom)
                {
                    if (_updateSubscriptions.TryGetValue(symbol, out UpdateSubscription value))
                    {
                        try
                        {
                            await _binanceSocketClients.Unsubscribe(value).ConfigureAwait(true);
                            _updateSubscriptions.Remove(symbol);

                            _logger.Information($"Unsubscribed from {symbol}");
                        }
                        catch
                        {
                            // Nothing
                        }
                    }
                }
            }

            // Subscribe
            if (subscribeTo.Any())
            {
                _logger.Information($"Symbols to subscribe to are {string.Join(',', subscribeTo)}");

                foreach (var symbol in subscribeTo)
                {
                    try
                    {
                        // Subscribe to all symbols
                        var updateSubscription = await _binanceSocketClients.Spot.SubscribeToBookTickerUpdatesAsync(symbol, async (BinanceStreamBookPrice bsbp) =>
                        {
                            var askPriceAdded = false;
                            var bidPriceAdded = false;

                            // Update ask price
                            while (!askPriceAdded)
                            {
                                if (_bestAskPrices.ContainsKey(bsbp.Symbol))
                                {
                                    await _bestAskPrices[bsbp.Symbol].Add(bsbp.BestAskPrice).ConfigureAwait(true);
                                    askPriceAdded = true;
                                }
                                else
                                {
                                    var bestAskPrice = new BestPrice(bsbp.Symbol);
                                    askPriceAdded = _bestAskPrices.TryAdd(bsbp.Symbol, bestAskPrice);
                                    await bestAskPrice.Add(bsbp.BestAskPrice).ConfigureAwait(true);
                                }

                                _logger.Verbose($"{bsbp.Symbol}: Book tick update - asking is {bsbp.BestAskPrice:0.00}");
                            }

                            // Update bid price
                            while (!bidPriceAdded)
                            {
                                if (_bestBidPrices.ContainsKey(bsbp.Symbol))
                                {
                                    await _bestBidPrices[bsbp.Symbol].Add(bsbp.BestBidPrice).ConfigureAwait(true);
                                    bidPriceAdded = true;
                                }
                                else
                                {
                                    var bestBidPrice = new BestPrice(bsbp.Symbol);
                                    bidPriceAdded = _bestBidPrices.TryAdd(bsbp.Symbol, bestBidPrice);
                                    await bestBidPrice.Add(bsbp.BestBidPrice).ConfigureAwait(true);
                                }

                                _logger.Verbose($"{bsbp.Symbol}: Book tick update - bidding is {bsbp.BestBidPrice:0.00}");
                            }
                        }).ConfigureAwait(true);

                        if (!updateSubscription.Success)
                        {
                            throw new Exception($"Subscribing to {symbol} failed");
                        }

                        _updateSubscriptions.Add(symbol, updateSubscription.Data);
                        _logger.Information($"Subscribed to {symbol}");
                    }
                    catch (Exception e)
                    {
                        _logger.Fatal($"Connecting to Binance failed, retrying...");
                        _logger.Fatal(e, e.Message);
                    }
                }
            }

            _logger.Debug($"Subscriptions checked");
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

            _ = recommendation ?? throw new ArgumentNullException(paramName: nameof(recommendation));

            #endregion

            _recommendations.AddOrUpdate(recommendation.Symbol, recommendation, (key, value) => value = recommendation);
        }

        /// <summary>
        /// Returns a recommendation for a <paramref name="symbol"/>.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public Recommendation GetRecommendation(string symbol)
        {
            if (!_recommendations.ContainsKey(symbol))
            {
                return new Recommendation() { Action = datalayer.Enums.Action.Hold };
            }

            return _recommendations[symbol];
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

            if (_openOrders.ContainsKey(openOrder.Id))
            {
                _openOrders.Remove(openOrder.Id);
            }

            _logger.Debug($"Order added {openOrder.Id}");

            _openOrders.Add(openOrder.Id, openOrder);
        }

        /// <summary>
        /// Removes an open order
        /// </summary>
        /// <param name="clientOrderId">Id of open order</param>
        public void RemoveOpenOrder(string clientOrderId)
        {
            _logger.Debug($"Order removed {clientOrderId}");

            _openOrders.Remove(clientOrderId);
        }

        /// <summary>
        /// Returns the currently blocked
        /// </summary>
        public decimal GetOpenOrderValue(string symbol)
        {
            // Remove old orders
            foreach (var oo in _openOrders.Where(o => o.Value.CreatedOn < DateTime.UtcNow.AddSeconds(-10)).Select(o => o.Key))
            {
                _logger.Debug($"Order cleaned {oo}");
                _openOrders.Remove(oo);
            }

            return _openOrders.Where(o => o.Value.Symbol == symbol).Sum(o => o.Value.Quantity);
        }

        /// <summary>
        /// Stats3s
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public Stats3s Stats3sFor(string symbol)
        {
            // Reference current buffer data
            var s3s = _stats3s;

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
            var s15s = _stats15s;

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
            var s2m = _stats2m;

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
            var s10m = _stats10m;

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
            var s2h = _stats2h;

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
            if (_stats10m == null)
            {
                return new List<string>();
            }

            return _stats10m.Select(t => t.Symbol);
        }

        /// <summary>
        /// Returns the latest ask price for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Ask price of the symbol</returns>
        public decimal GetAskPrice(string symbol)
        {
            if (!_bestAskPrices.ContainsKey(symbol))
            {
                _logger.Debug($"{symbol}: No asking price available");

                // Get price from Binance
                var result = _binanceClient.Spot.Market.GetPrice(symbol);
                if (result.Success)
                {
                    return result.Data.Price;
                }

                _logger.Warning($"{symbol}: Could not fetch price from Binance");

                return -1;
            }
            else
            {
                return _bestAskPrices[symbol].GetAverage();
            }
        }

        /// <summary>
        /// Returns the latest bid price for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Bid price of the symol</returns>
        public decimal GetBidPrice(string symbol)
        {
            if (!_bestBidPrices.ContainsKey(symbol))
            {
                _logger.Warning($"{symbol}: No bidding price available");
                return -1;
            }
            else
            {
                return _bestBidPrices[symbol].GetAverage();
            }
        }

        /// <summary>
        /// Returns exchange information for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Exchange information</returns>
        public BinanceSymbol GetSymbolInfoFor(string symbol)
        {
            if (_binanceExchangeInfo == null || string.IsNullOrEmpty(symbol))
            {
                return null;
            }

            var symbolInfo = _binanceExchangeInfo.Symbols.FirstOrDefault(s => s.Name == symbol);

            if (symbolInfo == null || symbolInfo.Status != SymbolStatus.Trading)
            {
                _logger.Warning($"{symbol}: No exchange info available");
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
            var latest = _latestMA10mAnd30mCrossing;
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
            var latest = _latestMA30mAnd1hCrossing;
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
            var latest = _latestMA1hAnd3hCrossing;
            return latest.FirstOrDefault(s => s.Symbol == symbol);
        }

        /// <summary>
        /// Returns the last falling price
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public FallingPrice GetLastFallingPrice(string symbol)
        {
            var fallingPrice = _fallingPrices.GetValueOrDefault(symbol);
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
        public Task Start()
        {
            _logger.Information("Starting Buffer");

            // Initial loading
            _logger.Debug("Preloading buffer");

            var availableSymbols = new List<string>();
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var loadingTasks = new List<Task>();
                var database = Program.Container.GetService<TrapeContext>();

                loadingTasks.Add(new Task(async () => _stats3s = await database.Get3SecondsTrendAsync().ConfigureAwait(true)));
                loadingTasks.Add(new Task(async () => _stats15s = await database.Get15SecondsTrendAsync().ConfigureAwait(true)));
                loadingTasks.Add(new Task(async () => _stats2m = await database.Get2MinutesTrendAsync().ConfigureAwait(true)));
                loadingTasks.Add(new Task(async () => _stats10m = await database.Get10MinutesTrendAsync().ConfigureAwait(true)));
                loadingTasks.Add(new Task(async () => _stats2h = await database.Get2HoursTrendAsync().ConfigureAwait(true)));
                loadingTasks.Add(new Task(async () => _latestMA10mAnd30mCrossing = await database.GetLatestMA10mAndMA30mCrossing().ConfigureAwait(true)));

                // Wait till loading completes
                Task.WaitAll(loadingTasks.ToArray());
            }

            _logger.Debug("Buffer preloaded");

            // Precheck
            CheckSubscriptions();

            // Starting of timers
            _jobSymbolChecker.Start();
            _jobStats3s.Start();
            _jobStats15s.Start();
            _jobStats2m.Start();
            _jobStats10m.Start();
            _jobStats2h.Start();
            _jobForCrossings.Start();
            _jobExchangeInfo.Start();

            // Loading exchange information
            ExchangeInfo();

            _logger.Information("Buffer started");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops a buffer
        /// </summary>
        public void Terminate()
        {
            _logger.Information("Stopping buffer");

            // Shutdown of timers
            _jobStats3s.Terminate();
            _jobStats15s.Terminate();
            _jobStats2m.Terminate();
            _jobStats10m.Terminate();
            _jobStats2h.Terminate();
            _jobForCrossings.Terminate();
            _jobExchangeInfo.Terminate();
            _jobSymbolChecker.Terminate();

            // Signal cancellation for what ever remains
            _cancellationTokenSource.Cancel();

            // Close connections
            _binanceSocketClients.UnsubscribeAll();
            _logger.Information("Buffer stopped");
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
                _jobStats3s.Dispose();
                _jobStats15s.Dispose();
                _jobStats2m.Dispose();
                _jobStats10m.Dispose();
                _jobStats2h.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
