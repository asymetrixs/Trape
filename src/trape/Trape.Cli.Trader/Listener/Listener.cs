using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketData;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.trader.Listener.Models;
using Trape.Datalayer.Models;
using Trape.Jobs;

namespace Trape.Cli.trader.Listener
{
    /// <summary>
    /// This class is an implementation of <c>IBuffer</c>
    /// </summary>
    public class Listener : IListener
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
        /// A list of all known currencies
        /// </summary>
        private readonly HashSet<string> _assets;

        /// <summary>
        /// New assets
        /// </summary>
        private readonly Subject<BinanceSymbol> _newAssets;

        /// <summary>
        /// Binance Client
        /// </summary>
        private readonly IBinanceClient _binanceClient;

        /// <summary>
        /// Binance Socket Client
        /// </summary>
        private readonly IBinanceSocketClient _binanceSocketClients;

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

        /// <summary>
        /// Indicates that service is starting
        /// </summary>
        private bool _starting;

        /// <summary>
        /// Checks the exchange infos for new assets
        /// </summary>
        private readonly Job _jobExchangeInfo;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Buffer</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="binanceClient">Binance Client</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public Listener(ILogger logger, IBinanceClient binanceClient, IBinanceSocketClient binanceSocketClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            _binanceSocketClients = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            #endregion

            _logger = logger.ForContext<Listener>();
            _cancellationTokenSource = new CancellationTokenSource();
            _disposed = false;
            _binanceExchangeInfo = null;
            _assets = new HashSet<string>();
            _newAssets = new Subject<BinanceSymbol>();
            _starting = true;

            #region Job setup

            // Set up all jobs that query the Database in different
            // intervals to get recent data
            _jobExchangeInfo = new Job(new TimeSpan(0, 0, 1), async () => await ExchangeInfo().ConfigureAwait(true), _cancellationTokenSource.Token);

            #endregion
        }

        #endregion

        public IObservable<BinanceSymbol> NewAssets => _newAssets;

        #region Jobs

        /// <summary>
        /// Updates Exchange Information
        /// </summary>
        private async Task ExchangeInfo()
        {
            var result = await _binanceClient.Spot.System.GetExchangeInfoAsync(_cancellationTokenSource.Token).ConfigureAwait(true);

            if (result.Success)
            {
                _binanceExchangeInfo = result.Data;

                for (int i = 0; i < result.Data.Symbols.Count(); i++)
                {
                    var current = result.Data.Symbols.ElementAt(i);

                    if (current.QuoteAsset == "USDT" && !_assets.Contains(current.BaseAsset))
                    {
                        _assets.Add(current.BaseAsset);

                        if (!_starting)
                        {
                            _logger.Information($"{current.BaseAsset}: New asset detected");
                            _newAssets.OnNext(current);
                        }
                    }
                }

                if (_starting)
                {
                    _logger.Information($"{result.Data.Symbols.Count()} assets detected");
                }
            }
        }

        ///// <summary>
        ///// Checks symbols
        ///// </summary>
        ///// <returns></returns>
        //private async void AddSubscription(BinanceSymbol symbol)
        //{
        //    // Initial loading
        //    _logger.Debug("Checking subscriptions");

        //    // Subscribe to all symbols
        //    var updateSubscription = await _binanceSocketClients.Spot.SubscribeToAllBookTickerUpdatesAsync(async (BinanceStreamBookPrice bsbp) =>
        //    {
        //        var askPriceAdded = false;
        //        var bidPriceAdded = false;

        //        // Only go for pairs traded against USDT
        //        if (!bsbp.Symbol.EndsWith("USDT", StringComparison.InvariantCulture))
        //        {
        //            return;
        //        }

        //        // Only go for low value assets if asset was not added before
        //        if (!_currentPrices.ContainsKey(bsbp.Symbol) && bsbp.BestAskPrice > 2)
        //        {
        //            return;
        //        }

        //        // Remove USDT
        //        bsbp.Symbol = bsbp.Symbol.Replace("USDT", string.Empty, StringComparison.InvariantCulture);

        //        if (bsbp.Symbol.Contains("TUSD", StringComparison.InvariantCulture)
        //        || bsbp.Symbol.Contains("BUSD", StringComparison.InvariantCulture)
        //        || bsbp.Symbol.Contains("USDC", StringComparison.InvariantCulture)
        //        || bsbp.Symbol.Contains("EUR", StringComparison.InvariantCulture))
        //        {
        //            return;
        //        }

        //        // Add price
        //        if (!_currentPrices.ContainsKey(bsbp.Symbol))
        //        {
        //            lock (_currentPrices)
        //            {
        //                _currentPrices[bsbp.Symbol] = new ConcurrentQueue<CurrentBookPrice>();
        //            }
        //        }
        //        _currentPrices[bsbp.Symbol].Enqueue(new CurrentBookPrice(bsbp));

        //        // Update ask price
        //        if (_bestAskPrices.ContainsKey(bsbp.Symbol))
        //        {
        //            await _bestAskPrices[bsbp.Symbol].Add(bsbp.BestAskPrice).ConfigureAwait(true);
        //        }
        //        else
        //        {
        //            var bestAskPrice = new BestPrice(bsbp.Symbol);
        //            askPriceAdded = _bestAskPrices.TryAdd(bsbp.Symbol, bestAskPrice);
        //            await bestAskPrice.Add(bsbp.BestAskPrice).ConfigureAwait(true);
        //        }

        //        _logger.Verbose($"{bsbp.Symbol}: Book tick update - asking is {bsbp.BestAskPrice:0.00}");

        //        if (_bestBidPrices.ContainsKey(bsbp.Symbol))
        //        {
        //            await _bestBidPrices[bsbp.Symbol].Add(bsbp.BestBidPrice).ConfigureAwait(true);
        //            bidPriceAdded = true;
        //        }
        //        else
        //        {
        //            var bestBidPrice = new BestPrice(bsbp.Symbol);
        //            bidPriceAdded = _bestBidPrices.TryAdd(bsbp.Symbol, bestBidPrice);
        //            await bestBidPrice.Add(bsbp.BestBidPrice).ConfigureAwait(true);
        //        }

        //        _logger.Verbose($"{bsbp.Symbol}: Book tick update - bidding is {bsbp.BestBidPrice:0.00}");

        //    }).ConfigureAwait(true);

        //    if (!updateSubscription.Success)
        //    {
        //        _logger.Warning("Subscription update failed");
        //        return;
        //    }

        //    _updateSubscriptions = updateSubscription.Data;
        //    _logger.Information("Subscribed");

        //    _logger.Debug("Subscriptions checked");
        //}

        #endregion

        #region Methods


        ///// <summary>
        ///// Returns the change in percent in a given timespan compared to now.
        ///// </summary>
        ///// <param name="symbol">Symbol</param>
        ///// <param name="timespan">Interval</param>
        ///// <returns></returns>
        //public decimal? Slope(string symbol, TimeSpan timespan)
        //{
        //    var ordered = _currentPrices[symbol].Where(d => d.On >= DateTime.Now.Add(timespan)).OrderByDescending(s => s.On);
        //    var latest = ordered.FirstOrDefault();
        //    var oldest = ordered.LastOrDefault();

        //    if (latest is null || oldest is null)
        //    {
        //        return null;
        //    }

        //    // normalize
        //    var divider = latest.On.Ticks - oldest.On.Ticks;

        //    if (divider == 0)
        //    {
        //        return null;
        //    }

        //    return (latest.BestAskPrice - oldest.BestAskPrice) / divider;
        //}

        ///// <summary>
        ///// Returns lowest price in given timespan
        ///// </summary>
        ///// <param name="symbol">Symbol</param>
        ///// <param name="timespan">Interval</param>
        ///// <returns></returns>
        ///// <exception cref="ArgumentNullException"/>
        ///// <exception cref="InvalidOperationException"/>
        //public decimal GetLowestPrice(string symbol, TimeSpan timespan)
        //{
        //    return _currentPrices[symbol].Where(d => d.On >= DateTime.Now.Add(timespan)).Min(s => s.BestBidPrice);
        //}

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
        public async Task Start()
        {
            _logger.Information("Starting Buffer");

            _jobExchangeInfo.Start();

            // Loading exchange information
            await ExchangeInfo().ConfigureAwait(true);

            _logger.Information("Buffer started");

            _starting = false;
        }

        /// <summary>
        /// Stops a buffer
        /// </summary>
        public void Terminate()
        {
            _logger.Information("Stopping buffer");

            // Shutdown of timers
            _jobExchangeInfo.Terminate();

            _newAssets.OnCompleted();

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
                _jobExchangeInfo.Dispose();
                _newAssets.Dispose();
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
