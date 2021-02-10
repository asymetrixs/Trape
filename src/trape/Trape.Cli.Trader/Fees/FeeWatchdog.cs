using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.SpotData;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.Trader.Account;
using Trape.Cli.Trader.Cache.Models;
using Trape.Cli.Trader.Market;

namespace Trape.Cli.Trader.Fees
{
    /// <summary>
    /// Watchdog to always have sufficient BNBs available to pay fees
    /// </summary>
    public class FeeWatchdog : IFeeWatchdog, IDisposable
    {
        #region Fields

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Accountant
        /// </summary>
        private readonly IAccountant _accountant;

        /// <summary>
        /// Binance Client
        /// </summary>
        private readonly IBinanceClient _binanceClient;

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Stock Exchange
        /// </summary>
        private readonly IStockExchange _stockExchange;

        /// <summary>
        /// Symbol for fees
        /// </summary>
        private readonly string _feeSymbol;

        /// <summary>
        /// Stock Exchange subscriber
        /// </summary>
        private readonly IDisposable _stockExchangeSubscriber;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>FeeWatchdog</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="binanceClient">Binance Client</param>
        public FeeWatchdog(ILogger logger, IAccountant accountant, IBinanceClient binanceClient, IStockExchange stockExchange)
        {
            #region Arguments check

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            _binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            _stockExchange = stockExchange ?? throw new ArgumentNullException(paramName: nameof(stockExchange));

            #endregion

            _logger = logger.ForContext(typeof(FeeWatchdog));
            _cancellationTokenSource = new CancellationTokenSource();
            _feeSymbol = "BNBUSDT";

            _stockExchangeSubscriber = stockExchange.NewOrder.Subscribe(async (bpo) => await CheckBNB(bpo).ConfigureAwait(false));
        }

        #endregion

        #region BNB Checker

        private async Task CheckBNB(BinancePlacedOrder _)
        {
            // Get remaining BNB balance for trading
            var bnb = await _accountant.GetBalance(_feeSymbol.Replace("USDT", string.Empty, StringComparison.InvariantCulture)).ConfigureAwait(true);
            if (bnb == null)
            {
                // Something is oddly wrong, wait a bit
                _logger.Debug("Cannot retrieve account info");
                return;
            }

            var priceQuery = await _binanceClient.Spot.Market.GetPriceAsync(_feeSymbol).ConfigureAwait(true);

            if (!priceQuery.Success)
            {
                _logger.Warning($"{_feeSymbol}: Cannot retrieve price {priceQuery.Error?.Message}");
                return;
            }

            var currentPrice = priceQuery.Data.Price;

            // Threshold for buy is 30
            if (bnb.Free < 30)
            {
                _logger.Information($"{_feeSymbol}: Low. {bnb.Free} - issuing buy");

                // Fixed for now, buy for 55 USDT
                const int buy = 1;

                // Get merchant and place order
                await _stockExchange.PlaceOrder(new ClientOrder(_feeSymbol)
                {
                    Side = OrderSide.Buy,
                    Type = OrderType.Limit,
                    OrderResponseType = OrderResponseType.Full,
                    Quantity = buy,
                    Price = currentPrice,
                    TimeInForce = TimeInForce.ImmediateOrCancel
                }, _cancellationTokenSource.Token).ConfigureAwait(true);

                _logger.Information($"Issued buy of {buy} for {currentPrice} USDT each");
            }
            else
            {
                _logger.Debug($"Fees OK at {bnb.Free}");
            }
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            _logger.Information("Starting Fee Watchdog");

            // nothing

            _logger.Information("Fee Watchdog started");
        }

        /// <summary>
        /// Terminate
        /// </summary>
        public void Terminate()
        {
            _logger.Information("Fee Watchdog stopping");

            _cancellationTokenSource.Cancel();

            _logger.Information("Fee Watchdog stopped");
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
                _cancellationTokenSource.Dispose();
                _stockExchangeSubscriber.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
