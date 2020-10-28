using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketStream;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SimpleInjector.Lifestyles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using trape.datalayer;
using trape.jobs;
using trape.mapper;

namespace trape.cli.collector.DataCollection
{
    /// <summary>
    /// Manages connections to binance to retrieve data
    /// </summary>
    public class CollectionManager : BackgroundService
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Binance Socket Client
        /// </summary>
        private readonly IBinanceSocketClient _binanceSocketClient;

        /// <summary>
        /// Binance Stream Tick Buffer
        /// </summary>
        private readonly ActionBlock<IBinanceTick> _binanceStreamTickBuffer;

        /// <summary>
        /// Binance Stream Kline Data Buffer
        /// </summary>
        private readonly ActionBlock<IBinanceStreamKlineData> _binanceStreamKlineDataBuffer;

        /// <summary>
        /// Binance Stream Book Price
        /// </summary>
        private readonly ActionBlock<BinanceStreamBookPrice> _binanceBookTickBuffer;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Start/Stop synchronizer
        /// </summary>
        private readonly SemaphoreSlim _startStop;

        /// <summary>
        /// Synchronizer for collectors
        /// </summary>
        private readonly SemaphoreSlim _managing;

        /// <summary>
        /// Shutdown indicator
        /// </summary>
        private bool _shutdown;

        /// <summary>
        /// Checks periodically if symbols have to be added or dropped
        /// </summary>
        private readonly Job _jobSubscriptionManager;

        /// <summary>
        /// Holds list of active subscriptions
        /// </summary>
        private readonly Dictionary<string, List<UpdateSubscription>> _activeSubscriptions;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>CollectionManager</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public CollectionManager(ILogger logger, IBinanceSocketClient binanceSocketClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _binanceSocketClient = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            #endregion

            _logger = logger.ForContext<CollectionManager>();
            _disposed = false;
            _cancellationTokenSource = new CancellationTokenSource();
            _startStop = new SemaphoreSlim(1, 1);
            _managing = new SemaphoreSlim(1, 1);
            _shutdown = false;
            _activeSubscriptions = new Dictionary<string, List<UpdateSubscription>>();

            #region Timer Setup

            // Check every five seconds
            _jobSubscriptionManager = new Job(new TimeSpan(0, 0, 5), async () => await Manage().ConfigureAwait(true));

            #endregion

            #region Buffer setup

            _binanceStreamTickBuffer = new ActionBlock<IBinanceTick>(async message => await Save(message, _cancellationTokenSource.Token).ConfigureAwait(true),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = _cancellationTokenSource.Token,
                    SingleProducerConstrained = false
                }
            );

            _binanceStreamKlineDataBuffer = new ActionBlock<IBinanceStreamKlineData>(async message => await Save(message, _cancellationTokenSource.Token).ConfigureAwait(true),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = _cancellationTokenSource.Token,
                    SingleProducerConstrained = false
                });

            _binanceBookTickBuffer = new ActionBlock<BinanceStreamBookPrice>(async message => await Save(message, _cancellationTokenSource.Token).ConfigureAwait(true),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = _cancellationTokenSource.Token,
                    SingleProducerConstrained = false
                });

            #endregion
        }

        /// <summary>
        /// Manages subscribing and unsubscribing
        /// </summary>
        /// <returns></returns>
        private async Task Manage()
        {
            // Stop timer, because it may take a while
            if (_managing.CurrentCount == 0)
            {
                return;
            }

            _managing.Wait();

            _logger.Verbose("Checking subscriptions");

            // Get symbols from database
            string[] requiredSymbols = default;

            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    requiredSymbols = database.Symbols.Where(s => s.IsCollectionActive).Select(s => s.Name).ToArray();
                }
                catch (Exception e)
                {
                    _logger.ForContext(typeof(CollectionManager));
                    _logger.Error(e, e.Message);
                    throw;
                }
            }

            try
            {
                if (requiredSymbols == null)
                {
                    _logger.Error("Cannot check subscriptions");
                    return;
                }

                // TODO: Sanity check if symbol exists on Binance
                _logger.Debug($"Holding {_activeSubscriptions.Count()} symbols with a total of {_activeSubscriptions.Sum(s => s.Value.Count)} subscriptions");

                // Subscribe to missing symbols
                foreach (var requiredSymbol in requiredSymbols)
                {
                    if (!_activeSubscriptions.ContainsKey(requiredSymbol))
                    {
                        await SubscribeTo(requiredSymbol).ConfigureAwait(true);
                    }
                }

                // Unsubscribe from non-required symbols
                foreach (var subscribedSymbol in _activeSubscriptions.Select(s => s.Key))
                {
                    if (!requiredSymbols.Contains(subscribedSymbol))
                    {
                        await UnsubscribeFrom(subscribedSymbol).ConfigureAwait(true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex.Message, ex);
            }
            finally
            {
                // Start timer again when done
                _managing.Release();
            }

            _logger.Verbose("Subscriptions checked");
        }

        #endregion

        #region Stream

        /// <summary>
        /// Saves <c>BinanceStreamTick</c> in the database.
        /// </summary>
        /// <param name="bt">Binance Stream Tick</param>
        /// <param name="cancellationToken">Canellation Token</param>
        /// <returns></returns>
        public static async Task Save(IBinanceTick bt, CancellationToken cancellationToken = default)
        {
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    database.Ticks.Add(Translator.Translate(bt));
                    await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    var logger = Program.Container.GetService<ILogger>();
                    logger.ForContext(typeof(CollectionManager));
                    logger.Error(e, e.Message);
                }
            }

            await Task.CompletedTask.ConfigureAwait(true);
        }

        /// <summary>
        /// Saves <c>BinanceStreamKlineData</c> in the database.
        /// </summary>
        /// <param name="bskd">Binance Stream Kline Data</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public static async Task Save(IBinanceStreamKlineData bskd, CancellationToken cancellationToken = default)
        {
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    database.Klines.Add(Translator.Translate(bskd));
                    await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    var logger = Program.Container.GetService<ILogger>();
                    logger.ForContext(typeof(CollectionManager));
                    logger.Error(e, e.Message);
                }
            }

            await Task.CompletedTask.ConfigureAwait(true);
        }

        /// <summary>
        /// Saves <c>BinanceStreamBookPrice</c> in the database
        /// </summary>
        /// <param name="bsbp">Binance Stream Book Price</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public static async Task Save(BinanceStreamBookPrice bsbp, CancellationToken cancellationToken = default)
        {
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    database.BookPrices.Add(Translator.Translate(bsbp));
                    await database.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    var logger = Program.Container.GetService<ILogger>();
                    logger.ForContext(typeof(CollectionManager));
                    logger.Error(e, e.Message);
                }
            }

            await Task.CompletedTask.ConfigureAwait(true);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the Collection Manager
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _startStop.WaitAsync().ConfigureAwait(true);

            _logger.Debug($"Acquired startup lock");

            // Setup cleanup
            Program.Container.GetService<IJobManager>().Start(new CleanUp());

            // Starting Subscription Manager
            _jobSubscriptionManager.Start();

            _startStop.Release();
            _logger.Debug($"Released startup lock");

            _logger.Verbose("Job Subscription Manager started");
        }

        /// <summary>
        /// Waits for process
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken = default)
        {
            _logger.Verbose("Waiting...");

            return _jobSubscriptionManager.WaitFor(stoppingToken);
        }

        /// <summary>
        /// Stops the Collection Manager
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _shutdown = true;

            // Check that start has finished/cancelled in case service is stopped while still starting
            await _startStop.WaitAsync().ConfigureAwait(true);

            // Check if timer is enabled.
            _jobSubscriptionManager.Terminate();

            try
            {
                _logger.Information($"Shutting down CollectionManager.");

                _logger.Debug($"Waiting for subscribers to terminate.");

                // Termintate subscriptions
                await _binanceSocketClient.UnsubscribeAll().ConfigureAwait(true);

                _logger.Debug($"Subscribers terminated.");

                // Signal buffer completion
                _binanceStreamTickBuffer.Complete();
                _binanceStreamKlineDataBuffer.Complete();
                _binanceBookTickBuffer.Complete();

                // Terminate jobs managed by job manager
                Program.Container.GetInstance<IJobManager>().TerminateAll();

                // Cancel all outstanding and running tasks
                _cancellationTokenSource.Cancel();

                // Signal stop to base
                await base.StopAsync(cancellationToken).ConfigureAwait(true);

                _logger.Information($"CollectionManager is down.");
            }
            catch
            {
                _logger.Error($"Ungraceful shutdown detected.");
                // nothing
            }
            finally
            {
                // Release lock
                _startStop.Release();
            }
        }

        /// <summary>
        /// Subscribes to updates for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        private async Task SubscribeTo(string symbol)
        {
            // Retry 30 times (e.g. network error, disconnect, etc.)
            int countTillHardExit = 30;

            var subscriptions = new List<UpdateSubscription>();
            var subscribeTasks = new List<Task<CallResult<UpdateSubscription>>>();


            while (countTillHardExit > 0)
            {
                _logger.Debug($"{symbol}: Starting collector");

                try
                {
                    // Subscribe to available streams

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToSymbolTickerUpdatesAsync(symbol, (IBinanceTick bt) =>
                    {
                        _binanceStreamTickBuffer.Post(bt);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneMinute, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThreeMinutes, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.FiveMinutes, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.FifteenMinutes, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThirtyMinutes, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneHour, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.TwoHour, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.SixHour, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.EightHour, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.TwelveHour, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneDay, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThreeDay, (IBinanceStreamKlineData bskd) =>
                    {
                        _binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(_binanceSocketClient.Spot.SubscribeToBookTickerUpdatesAsync(symbol, (BinanceStreamBookPrice bsbp) =>
                    {
                        _binanceBookTickBuffer.Post(bsbp);
                    }));

                    await Task.WhenAll(subscribeTasks.ToArray()).ConfigureAwait(true);

                    foreach (var t in subscribeTasks)
                    {
                        if (t.Result.Success)
                        {
                            subscriptions.Add(t.Result.Data);
                        }
                        else
                        {
                            throw new SubscriptionFailedException($"{symbol}: Problem to subscribe");
                        }
                    }

                    countTillHardExit = -1;
                }
                catch (Exception e)
                {
                    _logger.Fatal($"{symbol}: Connecting to Binance failed, retrying, {31 - countTillHardExit}/30");
                    _logger.Fatal(e, e.Message);

                    countTillHardExit--;

                    if (countTillHardExit == 0)
                    {
                        // Fail hard after 30 times and rely on service manager (e.g. systemd) to restart the service
                        _logger.Fatal("Shutting down, relying on systemd to restart");
                        Environment.Exit(1);
                    }
                }

                _logger.Debug($"{symbol}: Collector is started");

                // Register
                _activeSubscriptions.Add(symbol, subscriptions);
            }
        }

        /// <summary>
        /// Unsubscribes from updates for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        private async Task UnsubscribeFrom(string symbol)
        {
            _logger.Debug($"{symbol}: Stopping collector");

            var removedSubscriptions = new List<UpdateSubscription>();
            try
            {
                var unsubscribeTasks = new List<Task>();
                // Unsubscribe
                foreach (var subscription in _activeSubscriptions[symbol])
                {
                    // Return if in shutdown mode, everything will be unsubscribed than anyway
                    if (_shutdown) return;

                    unsubscribeTasks.Add(_binanceSocketClient.Unsubscribe(subscription));
                    removedSubscriptions.Add(subscription);
                }

                await Task.WhenAll(unsubscribeTasks).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                _logger.Debug($"{symbol}: Unsubscribing failed");
                _logger.Fatal(e, e.Message);
                return;
            }
            finally
            {
                _activeSubscriptions[symbol].RemoveAll(s => removedSubscriptions.Contains(s));
            }

            _logger.Debug($"{symbol}: Collector is stopped");

            // Remove
            _activeSubscriptions.Remove(symbol);
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public sealed override void Dispose()
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
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
