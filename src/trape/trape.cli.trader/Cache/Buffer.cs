using Binance.Net.Interfaces;
using Binance.Net.Objects;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;

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

        #region Timers

        private System.Timers.Timer _timerStats3s;

        private System.Timers.Timer _timerStats15s;

        private System.Timers.Timer _timerStats2m;

        private System.Timers.Timer _timerStats10m;

        private System.Timers.Timer _timerStats2h;

        private System.Timers.Timer _timerCurrentPrice;

        private System.Timers.Timer _timerExchangeInfo;

        #endregion

        #region Stats

        public IEnumerable<Stats3s> Stats3s { get; private set; }

        public IEnumerable<Stats15s> Stats15s { get; private set; }

        public IEnumerable<Stats2m> Stats2m { get; private set; }

        public IEnumerable<Stats10m> Stats10m { get; private set; }

        public IEnumerable<Stats2h> Stats2h { get; private set; }

        public IEnumerable<CurrentPrice> CurrentPrices { get; private set; }

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
            if (null == logger || null == binanceClient || null == binanceSocketClient)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger.ForContext<Buffer>();
            this._binanceClient = binanceClient;
            this._binanceSocketClient = binanceSocketClient;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._disposed = false;
            this._bestAskPrices = new ConcurrentDictionary<string, BestPrice>();
            this._bestBidPrices = new ConcurrentDictionary<string, BestPrice>();
            this._binanceExchangeInfo = null;

            #region Timer setup

            // Set up all timers that query the Database in different
            // intervals to get recent data

            this._timerStats3s = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 0, 0, 100).TotalMilliseconds
            };
            this._timerStats3s.Elapsed += _timerTrend3Seconds_Elapsed;

            this._timerStats15s = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 0, 0, 250).TotalMilliseconds
            };
            this._timerStats15s.Elapsed += _timerTrend15Seconds_Elapsed;

            this._timerStats2m = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 0, 0, 500).TotalMilliseconds
            };
            this._timerStats2m.Elapsed += _timerTrend2Minutes_Elapsed;

            this._timerStats10m = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 1).TotalMilliseconds
            };
            this._timerStats10m.Elapsed += _timerTrend10Minutes_Elapsed;

            this._timerStats2h = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 3).TotalMilliseconds
            };
            this._timerStats2h.Elapsed += _timerTrend2Hours_Elapsed;

            this._timerCurrentPrice = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 0, 0, 100).TotalMilliseconds
            };
            this._timerCurrentPrice.Elapsed += _timerCurrentPrice_Elapsed;

            this._timerExchangeInfo = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 1, 0).TotalMilliseconds
            };
            this._timerExchangeInfo.Elapsed += _timerExchangeInfo_Elapsed;

            #endregion
        }

        #endregion

        #region Timer elapsed

        /// <summary>
        /// Updates current prices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timerCurrentPrice_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._logger.Verbose("Updating current price");

            var database = Pool.DatabasePool.Get();
            this.CurrentPrices = await database.GetCurrentPriceAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated current price");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timerTrend3Seconds_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._logger.Verbose("Updating 3 seconds trend");

            var database = Pool.DatabasePool.Get();
            this.Stats3s = await database.Get3SecondsTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 3 seconds trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timerTrend15Seconds_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._logger.Verbose("Updating 15 seconds trend");

            var database = Pool.DatabasePool.Get();
            this.Stats15s = await database.Get15SecondsTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 15 seconds trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timerTrend2Minutes_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._logger.Verbose("Updating 2 minutes trend");

            var database = Pool.DatabasePool.Get();
            this.Stats2m = await database.Get2MinutesTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 2 minutes trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timerTrend10Minutes_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._logger.Verbose("Updating 10 minutes trend");

            var database = Pool.DatabasePool.Get();
            this.Stats10m = await database.Get10MinutesTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 10 minutes trend");
        }

        /// <summary>
        /// Updates trends
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timerTrend2Hours_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._logger.Verbose("Updating 2 hours trend");

            var database = Pool.DatabasePool.Get();
            this.Stats2h = await database.Get2HoursTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 2 hours trend");
        }

        /// <summary>
        /// Updates Exchange Information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timerExchangeInfo_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
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
        /// Returns the available symbols the buffer has data for
        /// </summary>
        /// <returns>List of symbols</returns>
        public IEnumerable<string> GetSymbols()
        {
            // Take symbols that are we have data for
            if (null == this.Stats2h)
            {
                return new List<string>();
            }

            return this.Stats2h.Where(t => t.IsValid()).Select(t => t.Symbol);
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
                this._logger.Warning($"No ask price for {symbol}");
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
                this._logger.Warning($"No bid price for {symbol}");
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

            var symbolInfo = this._binanceExchangeInfo.Symbols.SingleOrDefault(s => s.Name == symbol);

            if (null == symbolInfo || symbolInfo.Status != SymbolStatus.Trading)
            {
                this._logger.Warning($"No exchange info for {symbol}");
                return null;
            }

            return symbolInfo;
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
            var database = Pool.DatabasePool.Get();
            this.Stats3s = await database.Get3SecondsTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Stats15s = await database.Get15SecondsTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Stats2m = await database.Get2MinutesTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Stats10m = await database.Get10MinutesTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.Stats2h = await database.Get2HoursTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            this.CurrentPrices = await database.GetCurrentPriceAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);

            Pool.DatabasePool.Put(database);
            database = null;

            this._logger.Debug("Buffer preloaded");

            this._logger.Information($"Symbols to subscribe to are {String.Join(',', this.Stats2h.Select(s => s.Symbol))}, starting the subscription process");

            // Tries 30 times to subscribe to the ticker
            var countTillHardExit = 30;
            while (countTillHardExit > 0)
            {
                try
                {
                    // Subscribe to all symbols
                    await this._binanceSocketClient.SubscribeToBookTickerUpdatesAsync(this.Stats2h.Select(s => s.Symbol), (BinanceBookTick bbt) =>
                    {                        
                        var askPriceAdded = false;
                        var bidPriceAdded = false;

                        // Update ask price
                        while (!askPriceAdded)
                        {
                            if (this._bestAskPrices.ContainsKey(bbt.Symbol))
                            {
                                this._bestAskPrices[bbt.Symbol].Add(bbt.BestAskPrice);
                                askPriceAdded = true;
                            }
                            else
                            {
                                var bestAskPrice = new BestPrice(bbt.Symbol);
                                askPriceAdded = this._bestAskPrices.TryAdd(bbt.Symbol, bestAskPrice);
                                bestAskPrice.Add(bbt.BestAskPrice);
                            }

                            this._logger.Verbose($"Binance: book tick update - ask for {bbt.Symbol} is {bbt.BestAskPrice}");
                        }

                        // Update bid price
                        while (!bidPriceAdded)
                        {
                            if (this._bestBidPrices.ContainsKey(bbt.Symbol))
                            {
                                this._bestBidPrices[bbt.Symbol].Add(bbt.BestBidPrice);
                                bidPriceAdded = true;
                            }
                            else
                            {
                                var bestBidPrice = new BestPrice(bbt.Symbol);
                                bidPriceAdded = this._bestBidPrices.TryAdd(bbt.Symbol, bestBidPrice);
                                bestBidPrice.Add(bbt.BestBidPrice);
                            }

                            this._logger.Verbose($"Binance: book tick update - bid for {bbt.Symbol} is {bbt.BestBidPrice}");
                        }
                    }).ConfigureAwait(true);

                    countTillHardExit = -1;
                }
                catch (Exception e)
                {
                    this._logger.Fatal($"Connecting to Binance failed, retrying, {31 - countTillHardExit}/30");
                    this._logger.Fatal(e.Message, e);

                    countTillHardExit--;

                    // Log what stats are missing
                    #region Log invalid stats

                    if (this.Stats3s != null)
                    {
                        foreach (var s in this.Stats3s)
                        {
                            if (!s.IsValid())
                            {
                                this._logger.Warning($"3s: {s.Symbol} is invalid");
                            }
                            else
                            {
                                this._logger.Warning($"3s: {s.Symbol} is valid");
                            }
                        }
                    }
                    if (this.Stats15s != null)
                    {
                        foreach (var s in this.Stats3s)
                        {
                            if (!s.IsValid())
                            {
                                this._logger.Warning($"15s: {s.Symbol} is invalid");
                            }
                            else
                            {
                                this._logger.Warning($"15s: {s.Symbol} is valid");
                            }
                        }
                    }
                    if (this.Stats2m != null)
                    {
                        foreach (var s in this.Stats3s)
                        {
                            if (!s.IsValid())
                            {
                                this._logger.Warning($"2m: {s.Symbol} is invalid");
                            }
                            else
                            {
                                this._logger.Warning($"2m: {s.Symbol} is valid");
                            }
                        }
                    }
                    if (this.Stats10m != null)
                    {
                        foreach (var s in this.Stats3s)
                        {
                            if (!s.IsValid())
                            {
                                this._logger.Warning($"10m: {s.Symbol} is invalid");
                            }
                            else
                            {
                                this._logger.Warning($"10m: {s.Symbol} is valid");
                            }
                        }
                    }
                    if (this.Stats2h != null)
                    {
                        foreach (var s in this.Stats3s)
                        {
                            if (!s.IsValid())
                            {
                                this._logger.Warning($"2h: {s.Symbol} is invalid");
                            }
                            else
                            {
                                this._logger.Warning($"2h: {s.Symbol} is valid");
                            }
                        }
                    }

                    #endregion

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
            this._timerStats3s.Start();
            this._timerStats15s.Start();
            this._timerStats2m.Start();
            this._timerStats10m.Start();
            this._timerStats2h.Start();
            this._timerCurrentPrice.Start();

            // Loading exchange information
            this._timerExchangeInfo_Elapsed(null, null);

            this._logger.Information("Buffer started");
        }

        /// <summary>
        /// Stops a buffer
        /// </summary>
        public void Finish()
        {
            this._logger.Information("Stopping buffer");

            this._cancellationTokenSource.Cancel();

            // Shutdown of timers
            this._timerStats3s.Stop();
            this._timerStats15s.Stop();
            this._timerStats2m.Stop();
            this._timerStats10m.Stop();
            this._timerStats2h.Stop();
            this._timerCurrentPrice.Stop();

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
                this._timerStats3s.Dispose();
                this._timerStats15s.Dispose();
                this._timerStats2m.Dispose();
                this._timerStats10m.Dispose();
                this._timerStats2h.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
