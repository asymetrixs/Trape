namespace Trape.Cli.Trader.Listener
{
    using Binance.Net.Interfaces;
    using Binance.Net.Objects.Spot.MarketData;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using Trape.Jobs;

    /// <summary>
    /// This class is an implementation of <c>IBuffer</c>
    /// </summary>
    public class Listener : IListener
    {
        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// A list of all known currencies
        /// </summary>
        private readonly HashSet<string> _assets;

        /// <summary>
        /// New assets
        /// </summary>
        private readonly Subject<BinanceSymbol> _newAssets;

        /// <summary>
        /// New Exchange Infos
        /// </summary>
        private readonly Subject<BinanceExchangeInfo> _newExchangeInfo;

        /// <summary>
        /// Binance Client
        /// </summary>
        private readonly IBinanceClient _binanceClient;

        /// <summary>
        /// Binance Socket Client
        /// </summary>
        private readonly IBinanceSocketClient _binanceSocketClients;

        /// <summary>
        /// Checks the exchange infos for new assets
        /// </summary>
        private readonly Job _jobExchangeInfo;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Indicates that service is starting
        /// </summary>
        private bool _starting;

        /// <summary>
        /// Initializes a new instance of the <c>Buffer</c> class
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="binanceClient">Binance Client</param>
        /// <param name="binanceSocketClient">Binance Socket Client</param>
        public Listener(ILogger logger, IBinanceClient binanceClient, IBinanceSocketClient binanceSocketClient)
        {
            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            this._binanceSocketClients = binanceSocketClient ?? throw new ArgumentNullException(paramName: nameof(binanceSocketClient));

            this._logger = logger.ForContext<Listener>();
            this._cancellationTokenSource = new CancellationTokenSource();
            this._disposed = false;
            this._assets = new HashSet<string>();
            this._newAssets = new Subject<BinanceSymbol>();
            this._newExchangeInfo = new Subject<BinanceExchangeInfo>();
            this._starting = true;

            // Set up all jobs that query the Database in different
            // intervals to get recent data
            this._jobExchangeInfo = new Job(new TimeSpan(0, 0, 1), async () => await this.ExchangeInfo().ConfigureAwait(true), this._cancellationTokenSource.Token);
        }

        /// <summary>
        /// New Assets
        /// </summary>
        public IObservable<BinanceSymbol> NewAssets => this._newAssets;

        /// <summary>
        /// Exchange Infos
        /// </summary>
        public IObservable<BinanceExchangeInfo> NewExchangeInfo => this._newExchangeInfo;

        /// <summary>
        /// Starts the listener
        /// </summary>
        public async Task Start()
        {
            this._logger.Information("Starting Buffer");

            // Loading exchange information
            await this.ExchangeInfo().ConfigureAwait(true);

            this._jobExchangeInfo.Start();

            this._logger.Information("Buffer started");

            this._starting = false;
        }

        /// <summary>
        /// Stops the listener
        /// </summary>
        public void Terminate()
        {
            this._logger.Information("Stopping buffer");

            // Shutdown of timers
            this._jobExchangeInfo.Terminate();

            this._newAssets.OnCompleted();
            this._newExchangeInfo.OnCompleted();

            // Signal cancellation for what ever remains
            this._cancellationTokenSource.Cancel();

            // Close connections
            this._binanceSocketClients.UnsubscribeAll();
            this._logger.Information("Buffer stopped");
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._jobExchangeInfo.Dispose();
                this._newAssets.Dispose();
                this._cancellationTokenSource.Dispose();
                this._newExchangeInfo.Dispose();
            }

            this._disposed = true;
        }

        /// <summary>
        /// Updates Exchange Information
        /// </summary>
        private async Task ExchangeInfo()
        {
            var result = await this._binanceClient.Spot.System.GetExchangeInfoAsync(this._cancellationTokenSource.Token).ConfigureAwait(true);

            if (result.Success)
            {
                this._newExchangeInfo.OnNext(result.Data);

                for (int i = 0; i < result.Data.Symbols.Count(); i++)
                {
                    var current = result.Data.Symbols.ElementAt(i);

                    if (current.QuoteAsset == "USDT" && !this._assets.Contains(current.BaseAsset))
                    {
                        this._assets.Add(current.BaseAsset);

                        if (!this._starting)
                        {
                            this._logger.Information($"{current.BaseAsset}: New asset detected");
                            ////_newAssets.OnNext(current);
                        }
                    }
                }

                if (this._starting)
                {
                    this._logger.Information($"{result.Data.Symbols.Count()} assets detected");
                }
            }
        }
    }
}
