using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using trape.cli.collector.DataLayer;
using trape.jobs;

namespace trape.cli.collector.DataCollection
{
    public class CollectionManager : BackgroundService
    {
        #region Fields

        private ILogger _logger;

        private Dictionary<string, BinanceSocketClient> _binanceSocketClients;

        private ActionBlock<BinanceStreamTick> _binanceStreamTickBuffer;

        private ActionBlock<BinanceStreamKlineData> _binanceStreamKlineDataBuffer;

        private ActionBlock<BinanceBookTick> _binanceBookTickBuffer;

        private bool _disposed;

        private CancellationTokenSource _cancellationTokenSource;

        private SemaphoreSlim _startStop;

        private bool _shutdown;

        #endregion

        #region Constructor

        public CollectionManager(ILogger logger)
        {
            if (null == logger)
            {
                throw new ArgumentNullException("Paramter cannot be null");
            }

            this._logger = logger;
            this._binanceSocketClients = new Dictionary<string, BinanceSocketClient>();
            this._disposed = false;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._startStop = new SemaphoreSlim(1, 1);
            this._shutdown = false;

            #region Timer Setup

            this._binanceStreamTickBuffer = new ActionBlock<BinanceStreamTick>(async message => await Save(message, this._cancellationTokenSource.Token).ConfigureAwait(true),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = this._cancellationTokenSource.Token,
                    SingleProducerConstrained = false
                }
            );

            this._binanceStreamKlineDataBuffer = new ActionBlock<BinanceStreamKlineData>(async message => await Save(message, this._cancellationTokenSource.Token).ConfigureAwait(false),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = this._cancellationTokenSource.Token,
                    SingleProducerConstrained = false
                });

            this._binanceBookTickBuffer = new ActionBlock<BinanceBookTick>(async message => await Save(message, this._cancellationTokenSource.Token).ConfigureAwait(false),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = this._cancellationTokenSource.Token,
                    SingleProducerConstrained = false
                });

            #endregion
        }

        #endregion

        #region Timer Elapsed

        public static async Task Save(BinanceStreamTick bst, CancellationToken cancellationToken)
        {
            var database = Pool.DatabasePool.Get();
            await database.Insert(bst, cancellationToken).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);
        }

        public static async Task Save(BinanceStreamKlineData bskd, CancellationToken cancellationToken)
        {
            var database = Pool.DatabasePool.Get();
            await database.Insert(bskd, cancellationToken).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);
        }

        public static async Task Save(BinanceBookTick bbt, CancellationToken cancellationToken)
        {
            var database = Pool.DatabasePool.Get();
            await database.Insert(bbt, cancellationToken).ConfigureAwait(false);
            Pool.DatabasePool.Put(database);
        }
        
        #endregion

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._startStop.Wait();

            try
            {
                this._logger.Information("Setting up Collection Manager");

                var symbols = Configuration.GetValue("binance:symbols").Split(';', ',');

                await this._setupClients(symbols).ConfigureAwait(true);

                // Register cleanup job
                Program.Services.GetService<IJobManager>().Start(new CleanUp());

                this._logger.Information($"Collection Mangager is online with {this._binanceSocketClients.Count} clients.");
            }
            finally
            {
                this._startStop.Release();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            this._shutdown = true;

            await this._startStop.WaitAsync().ConfigureAwait(true);

            try
            {
                this._logger.Information($"Shutting down CollectionManager.");

                var terminateClients = new List<Task>();

                this._logger.Debug($"Waiting for subscribers to terminate.");
                foreach (var binanceSocketClient in this._binanceSocketClients)
                {
                    await binanceSocketClient.Value.UnsubscribeAll().ConfigureAwait(true);

                    this._logger.Debug($"{binanceSocketClient.Key} terminated.");
                }

                this._logger.Debug($"Subscribers terminated.");

                this._binanceStreamTickBuffer.Complete();
                this._binanceStreamKlineDataBuffer.Complete();
                this._binanceBookTickBuffer.Complete();
                
                Program.Services.GetRequiredService<IJobManager>().TerminateAll();

                this._cancellationTokenSource.Cancel();

                await base.StopAsync(cancellationToken).ConfigureAwait(true);

                this._logger.Information($"CollectionManager is down.");
            }
            catch
            {
                this._logger.Error($"Ungraceful shutdown detected.");
                // nothing
            }
            finally
            {
                this._startStop.Release();
            }
        }

        private async Task _setupClients(string[] symbols)
        {
            foreach (var symbol in symbols)
            {
                var socketClient = new BinanceSocketClient(new BinanceSocketClientOptions()
                {
                    ApiCredentials = new ApiCredentials(Configuration.GetValue("binance:apikey"),
                                                        Configuration.GetValue("binance:secretkey")),
                    AutoReconnect = true
                });

                this._binanceSocketClients.Add(symbol, socketClient);

                this._logger.Debug($"Starting collector for {symbol}");

                try
                {
                    if (this._shutdown) return;
                    await socketClient.SubscribeToSymbolTickerUpdatesAsync(symbol, (BinanceStreamTick bst) =>
                    {
                        this._binanceStreamTickBuffer.Post(bst);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to Symbol Ticker Updates");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneMinute, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 1 minute");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThreeMinutes, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 3 minutes");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.FiveMinutes, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 5 minutes");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.FifteenMinutes, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 15 minutes");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThirtyMinutes, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 30 minutes");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 1 hour");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.TwoHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 2 hours");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.SixHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 6 hours");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.EightHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 8 hours");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.TwelveHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 12 hours");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneDay, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 1 day");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThreeDay, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to KLine data 3 days");

                    if (this._shutdown) return;
                    await socketClient.SubscribeToBookTickerUpdatesAsync(symbol, (BinanceBookTick bbt) =>
                    {
                        this._binanceBookTickBuffer.Post(bbt);
                    }).ConfigureAwait(true);
                    this._logger.Debug($"{symbol}: Subscribed to Book Ticker");

                }
                catch (Exception e)
                {
                    this._logger.Fatal(e.Message, e);
                }

                this._logger.Debug($"Collectors online for {symbol}");
            }
        }

        #region Dispose

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public override sealed void Dispose()
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
