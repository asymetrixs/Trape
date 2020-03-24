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
    public class Buffer : IBuffer
    {
        #region Fields

        private ILogger _logger;

        private bool _disposed;

        private CancellationTokenSource _cancellationTokenSource;

        private ConcurrentDictionary<string, BestPrice> _bestAskPrices;

        private ConcurrentDictionary<string, BestPrice> _bestBidPrices;

        #region Timers

        private System.Timers.Timer _timerStats3s;

        private System.Timers.Timer _timerStats15s;

        private System.Timers.Timer _timerStats2m;

        private System.Timers.Timer _timerStats10m;

        private System.Timers.Timer _timerStats2h;

        private System.Timers.Timer _timerCurrentPrice;

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

        public Buffer(ILogger logger)
        {
            if (null == logger)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._disposed = false;
            this._bestAskPrices = new ConcurrentDictionary<string, BestPrice>();
            this._bestBidPrices = new ConcurrentDictionary<string, BestPrice>();

            #region Timer setup

            this._timerStats3s = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 1).TotalMilliseconds
            };
            this._timerStats3s.Elapsed += _timerTrend3Seconds_Elapsed;


            this._timerStats15s = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 5).TotalMilliseconds
            };
            this._timerStats15s.Elapsed += _timerTrend15Seconds_Elapsed;

            this._timerStats2m = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 1, 0).TotalMilliseconds
            };
            this._timerStats2m.Elapsed += _timerTrend2Minutes_Elapsed;

            this._timerStats10m = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 3, 0).TotalMilliseconds
            };
            this._timerStats10m.Elapsed += _timerTrend10Minutes_Elapsed;

            this._timerStats2h = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 10, 0).TotalMilliseconds
            };
            this._timerStats2h.Elapsed += _timerTrend2Hours_Elapsed;

            this._timerCurrentPrice = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 1).TotalMilliseconds
            };
            this._timerCurrentPrice.Elapsed += _timerCurrentPrice_Elapsed;

            #endregion
        }

        #endregion

        #region Timer elapsed

        private async void _timerCurrentPrice_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.CurrentPrices = await database.GetCurrentPriceAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated current price");
        }

        private async void _timerTrend3Seconds_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats3s = await database.Get3SecondsTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 3 seconds trend");
        }

        private async void _timerTrend15Seconds_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats15s = await database.Get15SecondsTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 15 seconds trend");
        }

        private async void _timerTrend2Minutes_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats2m = await database.Get2MinutesTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 2 minutes trend");
        }

        private async void _timerTrend10Minutes_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats10m = await database.Get10MinutesTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 10 minutes trend");
        }

        private async void _timerTrend2Hours_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();
            this.Stats2h = await database.Get2HoursTrendAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);
            Pool.DatabasePool.Put(database);

            this._logger.Verbose("Updated 2 hours trend");
        }

        #endregion

        public IEnumerable<string> GetSymbols()
        {
            // Take symbols that are we have data for
            if (null == this.Stats2h)
            {
                return new List<string>();
            }

            return this.Stats2h.Where(t => t.IsValid()).Select(t => t.Symbol);
        }

        public decimal GetAskPrice(string symbol)
        {
            if(!this._bestAskPrices.ContainsKey(symbol))
            {
                return -1;
            }
            else
            {
                return this._bestAskPrices[symbol].GetAverage();
            }
        }

        public decimal GetBidPrice(string symbol)
        {
            if (!this._bestBidPrices.ContainsKey(symbol))
            {
                return -1;
            }
            else
            {
                return this._bestBidPrices[symbol].GetAverage();
            }
        }

        #region Start / Stop

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

            var binanceSocketClient = Program.Services.GetService(typeof(IBinanceSocketClient)) as IBinanceSocketClient;

            await binanceSocketClient.SubscribeToBookTickerUpdatesAsync(this.GetSymbols(), (BinanceBookTick bbt) =>
            {
                lock (this)
                {
                    if (this._bestAskPrices.ContainsKey(bbt.Symbol))
                    {
                        this._bestAskPrices[bbt.Symbol].Add(bbt.BestAskPrice);
                    }
                    else
                    {
                        var bestAskPrice = new BestPrice(bbt.Symbol);
                        bestAskPrice.Add(bbt.BestAskPrice);
                        this._bestAskPrices.TryAdd(bbt.Symbol, bestAskPrice);
                    }

                    if (this._bestBidPrices.ContainsKey(bbt.Symbol))
                    {
                        this._bestBidPrices[bbt.Symbol].Add(bbt.BestBidPrice);
                    }
                    else
                    {
                        var bestBidPrice = new BestPrice(bbt.Symbol);
                        bestBidPrice.Add(bbt.BestBidPrice);
                        this._bestBidPrices.TryAdd(bbt.Symbol, bestBidPrice);
                    }
                }
            }).ConfigureAwait(true);

            this._logger.Debug($"Subscribed to Book Ticker");

            // Starting of timers
            this._timerStats3s.Start();
            this._timerStats15s.Start();
            this._timerStats2m.Start();
            this._timerStats10m.Start();
            this._timerStats2h.Start();
            this._timerCurrentPrice.Start();

            this._logger.Information("Buffer started");
        }

        public void Stop()
        {
            this._logger.Information("Stopping buffer");

            this._cancellationTokenSource.Cancel();

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
