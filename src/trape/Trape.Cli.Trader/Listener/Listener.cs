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
using Trape.Jobs;

namespace Trape.Cli.Trader.Listener
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
            _assets = new HashSet<string>();
            _newAssets = new Subject<BinanceSymbol>();
            _newExchangeInfo = new Subject<BinanceExchangeInfo>();
            _starting = true;

            #region Job setup

            // Set up all jobs that query the Database in different
            // intervals to get recent data
            _jobExchangeInfo = new Job(new TimeSpan(0, 0, 1), async () => await ExchangeInfo().ConfigureAwait(true), _cancellationTokenSource.Token);

            #endregion
        }

        #endregion

        /// <summary>
        /// New Assets
        /// </summary>
        public IObservable<BinanceSymbol> NewAssets => _newAssets;

        /// <summary>
        /// Exchange Infos
        /// </summary>
        public IObservable<BinanceExchangeInfo> NewExchangeInfo => _newExchangeInfo;

        #region Jobs

        /// <summary>
        /// Updates Exchange Information
        /// </summary>
        private async Task ExchangeInfo()
        {
            var result = await _binanceClient.Spot.System.GetExchangeInfoAsync(_cancellationTokenSource.Token).ConfigureAwait(true);

            if (result.Success)
            {
                _newExchangeInfo.OnNext(result.Data);
                
                for (int i = 0; i < result.Data.Symbols.Count(); i++)
                {
                    var current = result.Data.Symbols.ElementAt(i);

                    if (current.QuoteAsset == "USDT" && !_assets.Contains(current.BaseAsset))
                    {
                        _assets.Add(current.BaseAsset);

                        if (!_starting)
                        {
                            _logger.Information($"{current.BaseAsset}: New asset detected");
                            //_newAssets.OnNext(current);
                        }
                    }
                }

                if (_starting)
                {
                    _logger.Information($"{result.Data.Symbols.Count()} assets detected");
                }
            }
        }

        #endregion

        #region Methods

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
            _newExchangeInfo.OnCompleted();

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
