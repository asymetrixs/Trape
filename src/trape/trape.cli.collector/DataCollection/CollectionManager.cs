using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
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
    public class CollectionManager : IDisposable, ICollectionManager
    {
        private ILogger _logger;

        private Dictionary<string, BinanceSocketClient> _binanceSocketClients;

        private ActionBlock<BinanceStreamTick> _binanceStreamTickBuffer;

        private ActionBlock<BinanceStreamKlineData> _binanceStreamKlineDataBuffer;

        private ActionBlock<BinanceBookTick> _binanceBookTickBuffer;

        private IKillSwitch _killSwitch;

        private SemaphoreSlim _running;

        private CancellationTokenSource _cancellationTokenSource;

        private bool _disposed;

        public CollectionManager(ILogger logger, IKillSwitch killSwitch)
        {
            if (null == logger || null == killSwitch)
            {
                throw new ArgumentNullException("Paramter cannot be null");
            }

            this._logger = logger;
            this._binanceSocketClients = new Dictionary<string, BinanceSocketClient>();
            this._killSwitch = killSwitch;
            this._running = new SemaphoreSlim(0, 1);
            this._disposed = false;

            this._binanceStreamTickBuffer = new ActionBlock<BinanceStreamTick>(async message => await Save(message).ConfigureAwait(true),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = killSwitch.CancellationToken,
                    SingleProducerConstrained = false
                }
            );

            this._binanceStreamKlineDataBuffer = new ActionBlock<BinanceStreamKlineData>(async message => await Save(message).ConfigureAwait(false),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = killSwitch.CancellationToken,
                    SingleProducerConstrained = false
                });

            this._binanceBookTickBuffer = new ActionBlock<BinanceBookTick>(async message => await Save(message).ConfigureAwait(false),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = killSwitch.CancellationToken,
                    SingleProducerConstrained = false
                });
        }

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
                this._running.Dispose();
            }

            this._disposed = true;
        }

        public async Task Run(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;

            this._logger.Information("Setting up Collection Manager");

            var symbols = Configuration.GetValue("binance:symbols").Split(';', ',');

            foreach (var symbol in symbols)
            {
                var socketClient = new BinanceSocketClient(new BinanceSocketClientOptions()
                {
                    ApiCredentials = new ApiCredentials(Configuration.GetValue("binance:apikey"),
                    Configuration.GetValue("binance:secretkey")),
                    AutoReconnect = true
                });

                this._binanceSocketClients.Add(symbol, socketClient);

                this._logger.Verbose($"Starting collector for {symbol}");

                await socketClient.SubscribeToSymbolTickerUpdatesAsync(symbol, (BinanceStreamTick bst) =>
                {
                    this._binanceStreamTickBuffer.Post(bst);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneMinute, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                socketClient.SubscribeToBookTickerUpdates(symbol, (BinanceBookTick bbt) =>
                {
                    this._binanceBookTickBuffer.Post(bbt);
                });
            }

            // Register cleanup job
            Service.Get<IJobManager>().Start(new CleanUp());

            this._logger.Information($"Collection Mangager is online with {this._binanceSocketClients.Count} clients.");

            await this._running.WaitAsync().ConfigureAwait(false);
        }

        public void Terminate()
        {
            var terminateClients = new List<Task>();

            foreach (var binanceSocketClient in this._binanceSocketClients)
            {
                terminateClients.Add(binanceSocketClient.Value.UnsubscribeAll());
            }

            _cancellationTokenSource.Cancel();

            Task.WaitAll(terminateClients.ToArray());

            this._running.Release();
        }

        public async Task Save(BinanceStreamTick bst)
        {
            var database = Service.Get<ICoinTradeContext>();
            await database.Insert(bst, this._killSwitch.CancellationToken).ConfigureAwait(false);
        }

        public async Task Save(BinanceStreamKlineData bskd)
        {
            var database = Service.Get<ICoinTradeContext>();
            await database.Insert(bskd, this._killSwitch.CancellationToken).ConfigureAwait(false);
        }

        public async Task Save(BinanceBookTick bbt)
        {
            var database = Service.Get<ICoinTradeContext>();
            await database.Insert(bbt, this._killSwitch.CancellationToken).ConfigureAwait(false);
        }
    }
}
