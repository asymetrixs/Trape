namespace Trape.Cli.Trader.Fees
{
    using Binance.Net.Enums;
    using Binance.Net.Interfaces;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Trape.Cli.Trader.Account;
    using Trape.Cli.Trader.Cache.Models;
    using Trape.Cli.Trader.Market;

    /// <summary>
    /// Watchdog to always have sufficient BNBs available to pay fees
    /// </summary>
    public class FeeWatchdog : IFeeWatchdog, IDisposable
    {
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
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Stock Exchange subscriber
        /// </summary>
        private IDisposable? _stockExchangeSubscriber;

        /// <summary>
        /// Initializes a new instance of the <c>FeeWatchdog</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="binanceClient">Binance Client</param>
        public FeeWatchdog(ILogger logger, IAccountant accountant, IBinanceClient binanceClient, IStockExchange stockExchange)
        {
            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            this._binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            this._stockExchange = stockExchange ?? throw new ArgumentNullException(paramName: nameof(stockExchange));

            this._logger = logger.ForContext(typeof(FeeWatchdog));
            this._cancellationTokenSource = new CancellationTokenSource();
            this._feeSymbol = "BNBUSDT";
            this._stockExchangeSubscriber = null;
        }

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            this._logger.Information("Starting Fee Watchdog");

            this._stockExchangeSubscriber = this._stockExchange.NewOrder.Subscribe(async (bpo) => await this.CheckBNB().ConfigureAwait(false));

            this._logger.Information("Fee Watchdog started");
        }

        /// <summary>
        /// Terminate
        /// </summary>
        public void Terminate()
        {
            this._logger.Information("Fee Watchdog stopping");

            this._cancellationTokenSource.Cancel();

            this._logger.Information("Fee Watchdog stopped");
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
                this._cancellationTokenSource.Dispose();
                this._stockExchangeSubscriber?.Dispose();
            }

            this._disposed = true;
        }

        /// <summary>
        /// Checks that sufficient BNB are available
        /// </summary>
        /// <returns></returns>
        private async Task CheckBNB()
        {
            // Get remaining BNB balance for trading
            var bnb = await this._accountant.GetBalance(this._feeSymbol.Replace("USDT", string.Empty, StringComparison.InvariantCulture)).ConfigureAwait(true);
            if (bnb == null)
            {
                // Something is oddly wrong, wait a bit
                this._logger.Debug("Cannot retrieve account info");
                return;
            }

            var priceQuery = await this._binanceClient.Spot.Market.GetPriceAsync(this._feeSymbol).ConfigureAwait(true);

            if (!priceQuery.Success)
            {
                this._logger.Warning($"{this._feeSymbol}: Cannot retrieve price {priceQuery.Error?.Message}");
                return;
            }

            var currentPrice = priceQuery.Data.Price;

            // Threshold for buy is 30
            if (bnb.Free < 30)
            {
                this._logger.Information($"{this._feeSymbol}: Low. {bnb.Free} - issuing buy");

                // Fixed for now, buy for 55 USDT
                const int buy = 1;

                // Get merchant and place order
                await this._stockExchange.PlaceOrder(new ClientOrder(this._feeSymbol)
                {
                    Side = OrderSide.Buy,
                    Type = OrderType.Limit,
                    OrderResponseType = OrderResponseType.Full,
                    Quantity = buy,
                    Price = currentPrice,
                    TimeInForce = TimeInForce.ImmediateOrCancel
                }, this._cancellationTokenSource.Token).ConfigureAwait(true);

                this._logger.Information($"Issued buy of {buy} for {currentPrice} USDT each");
            }
            else
            {
                this._logger.Debug($"Fees OK at {bnb.Free}");
            }
        }
    }
}
