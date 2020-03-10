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
        private ILogger _logger;

        private Dictionary<string, BinanceSocketClient> _binanceSocketClients;

        private ActionBlock<BinanceStreamTick> _binanceStreamTickBuffer;

        private ActionBlock<BinanceStreamKlineData> _binanceStreamKlineDataBuffer;

        private ActionBlock<BinanceBookTick> _binanceBookTickBuffer;

        private bool _disposed;

        private CancellationTokenSource _cancellationTokenSource;

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
        }

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

        public static async Task Save(BinanceStreamTick bst, CancellationToken cancellationToken)
        {
            var database = Program.Services.GetService<ICoinTradeContext>();
            await database.Insert(bst, cancellationToken).ConfigureAwait(false);
        }

        public static async Task Save(BinanceStreamKlineData bskd, CancellationToken cancellationToken)
        {
            var database = Program.Services.GetService<ICoinTradeContext>();
            await database.Insert(bskd, cancellationToken).ConfigureAwait(false);
        }

        public static async Task Save(BinanceBookTick bbt, CancellationToken cancellationToken)
        {
            var database = Program.Services.GetService<ICoinTradeContext>();
            await database.Insert(bbt, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Information("Setting up Collection Manager");

            var symbols = Configuration.GetValue("binance:symbols").Split(';', ',');

            await this._setupClients(symbols).ConfigureAwait(false);

            // Register cleanup job

            Program.Services.GetService<IJobManager>().Start(new CleanUp());

            this._logger.Information($"Collection Mangager is online with {this._binanceSocketClients.Count} clients.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            var terminateClients = new List<Task>();

            foreach (var binanceSocketClient in this._binanceSocketClients)
            {
                terminateClients.Add(binanceSocketClient.Value.UnsubscribeAll());
            }

            Task.WaitAll(terminateClients.ToArray());

            this._binanceStreamTickBuffer.Complete();
            this._binanceStreamKlineDataBuffer.Complete();
            this._binanceBookTickBuffer.Complete();

            Program.Services.GetRequiredService<IJobManager>().TerminateAll();

            await base.StopAsync(cancellationToken).ConfigureAwait(false);
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

                this._logger.Verbose($"Starting collector for {symbol}");

                await socketClient.SubscribeToSymbolTickerUpdatesAsync(symbol, (BinanceStreamTick bst) =>
                {
                    this._binanceStreamTickBuffer.Post(bst);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneMinute, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThreeMinutes, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.FiveMinutes, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.FifteenMinutes, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThirtyMinutes, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneHour, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.TwoHour, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.SixHour, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.EightHour, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.TwelveHour, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneDay, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                await socketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThreeDay, (BinanceStreamKlineData bskd) =>
                {
                    this._binanceStreamKlineDataBuffer.Post(bskd);
                }).ConfigureAwait(false);

                //socketClient.SubscribeToBookTickerUpdates(symbol, (BinanceBookTick bbt) =>
                //{
                //    this._binanceBookTickBuffer.Post(bbt);
                //});
            }
        }
    }
}
