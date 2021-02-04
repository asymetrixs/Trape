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
using Trape.Cli.trader.Cache.Models;
using Trape.Cli.Trader.Cache.Models;
using Trape.Datalayer;
using Trape.Datalayer.Models;
using Trape.Jobs;

namespace Trape.Cli.trader.Cache
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
        /// Holds all prices of the last 30 minutes
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentQueue<CurrentBookPrice>> _currentPrices;

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
        private UpdateSubscription _updateSubscriptions;

        /// <summary>
        /// Exchange Information
        /// </summary>
        private BinanceExchangeInfo _binanceExchangeInfo;

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

        private readonly Job _jobExchangeInfo;

        private readonly Job _jobSymbolChecker;

        private readonly Job _jobQueueCleanup;

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
            _currentPrices = new ConcurrentDictionary<string, ConcurrentQueue<CurrentBookPrice>>();
            _binanceExchangeInfo = null;
            _fallingPrices = new Dictionary<string, FallingPrice>();
            _openOrders = new Dictionary<string, OpenOrder>();
            _recommendations = new ConcurrentDictionary<string, Recommendation>();

            #region Job setup

            // Set up all jobs that query the Database in different
            // intervals to get recent data
            _jobExchangeInfo = new Job(new TimeSpan(0, 1, 0), ExchangeInfo, _cancellationTokenSource.Token);
            _jobSymbolChecker = new Job(new TimeSpan(0, 0, 10), CheckSubscriptions, _cancellationTokenSource.Token);
            _jobQueueCleanup = new Job(new TimeSpan(0, 0, 5), QueueCleanup, _cancellationTokenSource.Token);

            #endregion
        }

        #endregion

        #region Jobs

        private void QueueCleanup()
        {
            // Get each list
            foreach (var list in _currentPrices.Values)
            {
                while (list.TryPeek(out CurrentBookPrice result))
                {
                    // Check if oldest element is more recent than from 30 minutes ago
                    // If so, break
                    if (result.On > DateTime.Now.AddMinutes(-30))
                    {
                        break;
                    }
                    else
                    {
                        // Dequeue because item is old
                        list.TryDequeue(out _);
                    }
                }
            }
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
                    availableSymbols = database.Symbols.Select(s => s.Name).AsNoTracking().ToList();
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                }
            }

            if (availableSymbols.Count == 0)
            {
                _logger.Warning("No symbols found for subscription, aborting...");
                return;
            }

            // Subscribe to all symbols
            try
            {
                var updateSubscription = await _binanceSocketClients.Spot.SubscribeToAllBookTickerUpdatesAsync(async (BinanceStreamBookPrice bsbp) =>
                {
                    var askPriceAdded = false;
                    var bidPriceAdded = false;

                    // Only go for pairs traded against USDT
                    if (!bsbp.Symbol.EndsWith("USDT", StringComparison.InvariantCulture))
                    {
                        return;
                    }

                    // Only go for low value assets if asset was not added before
                    if (!_currentPrices.ContainsKey(bsbp.Symbol) && bsbp.BestAskPrice > 2)
                    {
                        return;
                    }

                    // Remove USDT
                    bsbp.Symbol = bsbp.Symbol.Replace("USDT", string.Empty, StringComparison.InvariantCulture);

                    if (bsbp.Symbol.Contains("TUSD", StringComparison.InvariantCulture)
                    || bsbp.Symbol.Contains("BUSD", StringComparison.InvariantCulture)
                    || bsbp.Symbol.Contains("USDC", StringComparison.InvariantCulture)
                    || bsbp.Symbol.Contains("EUR", StringComparison.InvariantCulture))
                    {
                        return;
                    }

                    //// Only go for ONE for testing
                    //if (bsbp.Symbol != "ONE")
                    //{
                    //    return;
                    //}

                    // Add price
                    if (!_currentPrices.ContainsKey(bsbp.Symbol))
                    {
                        lock (_currentPrices)
                        {
                            _currentPrices[bsbp.Symbol] = new ConcurrentQueue<CurrentBookPrice>();
                        }
                    }
                    _currentPrices[bsbp.Symbol].Enqueue(new CurrentBookPrice(bsbp));

                    // Update ask price
                    if (_bestAskPrices.ContainsKey(bsbp.Symbol))
                    {
                        await _bestAskPrices[bsbp.Symbol].Add(bsbp.BestAskPrice).ConfigureAwait(true);
                    }
                    else
                    {
                        var bestAskPrice = new BestPrice(bsbp.Symbol);
                        askPriceAdded = _bestAskPrices.TryAdd(bsbp.Symbol, bestAskPrice);
                        await bestAskPrice.Add(bsbp.BestAskPrice).ConfigureAwait(true);
                    }

                    _logger.Verbose($"{bsbp.Symbol}: Book tick update - asking is {bsbp.BestAskPrice:0.00}");

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

                }).ConfigureAwait(true);

                if (!updateSubscription.Success)
                {
                    throw new Exception($"Subscribing failed");
                }

                _updateSubscriptions = updateSubscription.Data;
                _logger.Information($"Subscribed");
            }
            catch (Exception e)
            {
                _logger.Fatal("Connecting to Binance failed, retrying...");
                _logger.Fatal(e, e.Message);
            }

            _logger.Debug("Subscriptions checked");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if enough data for processing is available
        /// </summary>
        /// <param name="symbol">Symbol to check</param>
        /// <returns></returns>
        public bool IsReady(string symbol)
        {
            // Check if key exists, value exists and value is older than 15 minutes
            return _currentPrices.ContainsKey(symbol)
                    && _currentPrices[symbol].TryPeek(out CurrentBookPrice currentBookPrice)
                    && currentBookPrice.On > DateTime.Now.AddMinutes(-15);
        }

        /// <summary>
        /// Returns the change in percent in a given timespan compared to now.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="timespan">Interval</param>
        /// <returns></returns>
        public decimal? Slope(string symbol, TimeSpan timespan)
        {
            var ordered = _currentPrices[symbol].Where(d => d.On >= DateTime.Now.Add(timespan)).OrderByDescending(s => s.On);
            var latest = ordered.FirstOrDefault();
            var oldest = ordered.LastOrDefault();

            if (latest is null || oldest is null)
            {
                return null;
            }

            // normalize
            var divider = latest.On.Ticks - oldest.On.Ticks;

            if (divider == 0)
            {
                return null;
            }

            return (latest.BestAskPrice - oldest.BestAskPrice) / divider;
        }

        /// <summary>
        /// Returns lowest price in given timespan
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="timespan">Interval</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        public decimal GetLowestPrice(string symbol, TimeSpan timespan)
        {
            return _currentPrices[symbol].Where(d => d.On >= DateTime.Now.Add(timespan)).Min(s => s.BestBidPrice);
        }

        /// <summary>
        /// Updates a recommendation
        /// </summary>
        /// <param name="recommendation"></param>
        public void UpdateRecommendation(Recommendation recommendation)
        {
            #region Argument checks

            _ = recommendation ?? throw new ArgumentNullException(paramName: nameof(recommendation));

            #endregion

            _recommendations.AddOrUpdate(recommendation.Symbol, recommendation, (_, value) => value = recommendation);
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
                return new Recommendation() { Action = Datalayer.Enums.Action.Hold };
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
        /// Returns the available symbols the buffer has data for
        /// </summary>
        /// <returns>List of symbols</returns>
        public IEnumerable<string> GetSymbols()
        {
            // Take symbols that are we have data for
            return _bestAskPrices.Keys;
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
        public BinanceSymbol? GetSymbolInfoFor(string symbol)
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
        /// Returns the last falling price
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public FallingPrice? GetLastFallingPrice(string symbol)
        {
            var fallingPrice = _fallingPrices.GetValueOrDefault(symbol);
            return fallingPrice ?? null;
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

            // Precheck
            CheckSubscriptions();

            // Starting of timers
            _jobSymbolChecker.Start();
            _jobExchangeInfo.Start();
            _jobQueueCleanup.Start();

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
            _jobExchangeInfo.Terminate();
            _jobSymbolChecker.Terminate();
            _jobQueueCleanup.Terminate();

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
                _jobSymbolChecker.Dispose();
                _jobExchangeInfo.Dispose();
                _jobQueueCleanup.Dispose();
                _currentPrices.Clear();
            }

            _disposed = true;
        }

        #endregion
    }
}
