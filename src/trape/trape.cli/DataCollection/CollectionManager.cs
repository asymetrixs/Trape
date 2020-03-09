using trape.cli.collector.DataLayer;
using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace trape.cli.collector.DataCollection
{
    public class CollectionManager
    {
        private ILogger _logger;

        private Dictionary<string, BinanceSocketClient> _binanceSocketClients;

        private ActionBlock<BinanceStreamTick> _binanceStreamTickBuffer;

        private ActionBlock<BinanceStreamKlineData> _binanceStreamKlineData;

        private IKillSwitch _killSwitch;

        public CollectionManager(ILogger logger, IKillSwitch killSwitch)
        {
            if (null == logger || null == killSwitch)
            {
                throw new ArgumentNullException("Paramter cannot be null");
            }

            this._logger = logger;
            this._binanceSocketClients = new Dictionary<string, BinanceSocketClient>();
            this._killSwitch = killSwitch;

            this._binanceStreamTickBuffer = new ActionBlock<BinanceStreamTick>(async message => await _Save(message).ConfigureAwait(true),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = killSwitch.CancellationToken,
                    SingleProducerConstrained = false
                }
            );

            this._binanceStreamKlineData = new ActionBlock<BinanceStreamKlineData>(async message => await _Save(message).ConfigureAwait(false),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = killSwitch.CancellationToken,
                    SingleProducerConstrained = false
                });
        }

        public async Task Run()
        {
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
                    this._binanceStreamKlineData.Post(bskd);
                }).ConfigureAwait(false);
            }

            this._logger.Information($"Collection Mangager is online with {this._binanceSocketClients.Count} clients.");
        }

        public void Terminate()
        {
            var terminateClients = new List<Task>();
            foreach (var binanceSocketClient in this._binanceSocketClients)
            {
                terminateClients.Add(binanceSocketClient.Value.UnsubscribeAll());
            }

            Task.WaitAll(terminateClients.ToArray());
        }

        private async Task _Save(BinanceStreamTick bst)
        {
            var database = Service.Get<ICoinTradeContext>();

            await database.Insert(bst, this._killSwitch.CancellationToken).ConfigureAwait(false);
        }

        private async Task _Save(BinanceStreamKlineData bskd)
        {
            var database = Service.Get<ICoinTradeContext>();
            Console.WriteLine("BSKD");
            await database.Insert(bskd, this._killSwitch.CancellationToken).ConfigureAwait(false);
        }

    }
}
