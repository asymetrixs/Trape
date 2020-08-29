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
        private readonly ActionBlock<BinanceStreamTick> _binanceStreamTickBuffer;

        /// <summary>
        /// Binance Stream Kline Data Buffer
        /// </summary>
        private readonly ActionBlock<BinanceStreamKlineData> _binanceStreamKlineDataBuffer;

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
        private CancellationTokenSource _cancellationTokenSource;

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
        private Job _jobSubscriptionManager;

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

            this._binanceSocketClient = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            #endregion

            this._logger = logger.ForContext<CollectionManager>();
            this._disposed = false;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._startStop = new SemaphoreSlim(1, 1);
            this._managing = new SemaphoreSlim(1, 1);
            this._shutdown = false;
            this._activeSubscriptions = new Dictionary<string, List<UpdateSubscription>>();

            #region Timer Setup

            // Check every five seconds
            this._jobSubscriptionManager = new Job(new TimeSpan(0, 0, 5), async () => await _manage().ConfigureAwait(true));

            #endregion

            #region Buffer setup

            this._binanceStreamTickBuffer = new ActionBlock<BinanceStreamTick>(async message => await Save(message, this._cancellationTokenSource.Token).ConfigureAwait(true),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = this._cancellationTokenSource.Token,
                    SingleProducerConstrained = false
                }
            );

            this._binanceStreamKlineDataBuffer = new ActionBlock<BinanceStreamKlineData>(async message => await Save(message, this._cancellationTokenSource.Token).ConfigureAwait(true),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = this._cancellationTokenSource.Token,
                    SingleProducerConstrained = false
                });

            this._binanceBookTickBuffer = new ActionBlock<BinanceStreamBookPrice>(async message => await Save(message, this._cancellationTokenSource.Token).ConfigureAwait(true),
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = this._cancellationTokenSource.Token,
                    SingleProducerConstrained = false
                });

            #endregion
        }

        /// <summary>
        /// Manages subscribing and unsubscribing
        /// </summary>
        /// <returns></returns>
        private async Task _manage()
        {
            // Stop timer, because it may take a while
            if (this._managing.CurrentCount == 0)
            {
                return;
            }

            this._managing.Wait();

            this._logger.Verbose("Checking subscriptions");

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
                    this._logger.ForContext(typeof(CollectionManager));
                    this._logger.Error(e, e.Message);
                    throw;
                }
            }

            try
            {
                if (requiredSymbols == null)
                {
                    this._logger.Error("Cannot check subscriptions");
                    return;
                }

                // TODO: Sanity check if symbol exists on Binance
                this._logger.Debug($"Holding {this._activeSubscriptions.Count()} symbols with a total of {this._activeSubscriptions.Sum(s => s.Value.Count)} subscriptions");

                // Subscribe to missing symbols
                foreach (var requiredSymbol in requiredSymbols)
                {
                    if (!this._activeSubscriptions.ContainsKey(requiredSymbol))
                    {
                        await this._subscribeTo(requiredSymbol).ConfigureAwait(true);
                    }
                }

                // Unsubscribe from non-required symbols
                foreach (var subscribedSymbol in this._activeSubscriptions.Select(s => s.Key))
                {
                    if (!requiredSymbols.Contains(subscribedSymbol))
                    {
                        await this._unsubscribeFrom(subscribedSymbol).ConfigureAwait(true);
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.Warning(ex.Message, ex);
            }
            finally
            {
                // Start timer again when done
                this._managing.Release();
            }

            this._logger.Verbose("Subscriptions checked");
        }

        #endregion

        #region Timer Elapsed

        /// <summary>
        /// Saves <c>BinanceStreamTick</c> in the database.
        /// </summary>
        /// <param name="bst">Binance Stream Tick</param>
        /// <param name="cancellationToken">Canellation Token</param>
        /// <returns></returns>
        public static async Task Save(BinanceStreamTick bst, CancellationToken cancellationToken = default)
        {
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    database.Ticks.Add(Translator.Translate(bst));
                    database.SaveChanges();
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
        public static async Task Save(BinanceStreamKlineData bskd, CancellationToken cancellationToken = default)
        {
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                var database = Program.Container.GetService<TrapeContext>();
                try
                {
                    database.Klines.Add(Translator.Translate(bskd));
                    database.SaveChanges();
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
                    database.BookTicks.Add(Translator.Translate(bsbp));
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
        public override Task StartAsync(CancellationToken cancellationToken = default)
        {
            this._startStop.Wait();

            this._logger.Debug($"Acquired startup lock");

            try
            {
                this._logger.Information("Setting up Collection Manager");

                // Register cleanup job and start
                Program.Container.GetService<IJobManager>().Start(new CleanUp());

                this._logger.Information($"Collection Mangager is online");
            }
            finally
            {
                this._logger.Debug($"Releasing startup lock");

                this._startStop.Release();
            }

            this._jobSubscriptionManager.Start();

            this._logger.Verbose("Job Subscription Manager started");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Waits for process
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken = default)
        {
            return this._jobSubscriptionManager.WaitFor();
        }

        /// <summary>
        /// Stops the Collection Manager
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken = default)
        {
            this._shutdown = true;

            // Check that start has finished/cancelled in case service is stopped while still starting
            await this._startStop.WaitAsync().ConfigureAwait(true);

            // Check if timer is enabled.
            this._jobSubscriptionManager.Terminate();
                        
            try
            {
                this._logger.Information($"Shutting down CollectionManager.");

                this._logger.Debug($"Waiting for subscribers to terminate.");

                // Termintate subscriptions
                await this._binanceSocketClient.UnsubscribeAll().ConfigureAwait(true);

                this._logger.Debug($"Subscribers terminated.");

                // Signal buffer completion
                this._binanceStreamTickBuffer.Complete();
                this._binanceStreamKlineDataBuffer.Complete();
                this._binanceBookTickBuffer.Complete();

                // Terminate jobs managed by job manager
                Program.Container.GetInstance<IJobManager>().TerminateAll();

                // Cancel all outstanding and running tasks
                this._cancellationTokenSource.Cancel();

                // Signal stop to base
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
                // Release lock
                this._startStop.Release();
            }
        }

        /// <summary>
        /// Subscribes to updates for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        private async Task _subscribeTo(string symbol)
        {
            // Retry 30 times (e.g. network error, disconnect, etc.)
            int countTillHardExit = 30;

            var subscriptions = new List<UpdateSubscription>();
            var subscribeTasks = new List<Task<CallResult<UpdateSubscription>>>();


            while (countTillHardExit > 0)
            {
                this._logger.Debug($"{symbol}: Starting collector");

                try
                {
                    // Subscribe to available streams

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToSymbolTickerUpdatesAsync(symbol, (BinanceStreamTick bst) =>
                    {
                        this._binanceStreamTickBuffer.Post(bst);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneMinute, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThreeMinutes, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.FiveMinutes, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.FifteenMinutes, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThirtyMinutes, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.TwoHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.SixHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.EightHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.TwelveHour, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.OneDay, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToKlineUpdatesAsync(symbol, KlineInterval.ThreeDay, (BinanceStreamKlineData bskd) =>
                    {
                        this._binanceStreamKlineDataBuffer.Post(bskd);
                    }));

                    subscribeTasks.Add(this._binanceSocketClient.SubscribeToBookTickerUpdatesAsync(symbol, (BinanceStreamBookPrice bsbp) =>
                    {
                        this._binanceBookTickBuffer.Post(bsbp);
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
                    this._logger.Fatal($"{symbol}: Connecting to Binance failed, retrying, {31 - countTillHardExit}/30");
                    this._logger.Fatal(e, e.Message);

                    countTillHardExit--;

                    if (countTillHardExit == 0)
                    {
                        // Fail hard after 30 times and rely on service manager (e.g. systemd) to restart the service
                        this._logger.Fatal("Shutting down, relying on systemd to restart");
                        Environment.Exit(1);
                    }
                }

                this._logger.Debug($"{symbol}: Collector is started");

                // Register
                this._activeSubscriptions.Add(symbol, subscriptions);
            }
        }

        /// <summary>
        /// Unsubscribes from updates for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        private async Task _unsubscribeFrom(string symbol)
        {
            this._logger.Debug($"{symbol}: Stopping collector");

            var removedSubscriptions = new List<UpdateSubscription>();
            try
            {
                var unsubscribeTasks = new List<Task>();
                // Unsubscribe
                foreach (var subscription in this._activeSubscriptions[symbol])
                {
                    // Return if in shutdown mode, everything will be unsubscribed than anyway
                    if (this._shutdown) return;

                    unsubscribeTasks.Add(this._binanceSocketClient.Unsubscribe(subscription));
                    removedSubscriptions.Add(subscription);
                }

                await Task.WhenAll(unsubscribeTasks).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                this._logger.Debug($"{symbol}: Unsubscribing failed");
                this._logger.Fatal(e, e.Message);
                return;
            }
            finally
            {
                this._activeSubscriptions[symbol].RemoveAll(s => removedSubscriptions.Contains(s));
            }

            this._logger.Debug($"{symbol}: Collector is stopped");

            // Remove
            this._activeSubscriptions.Remove(symbol);
        }

        #endregion

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
