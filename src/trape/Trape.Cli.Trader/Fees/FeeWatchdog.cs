using Serilog;
using System;
using System.Threading;
using trape.cli.trader.Account;
using trape.cli.trader.Cache;
using trape.cli.trader.Market;
using trape.datalayer.Models;
using trape.jobs;
using OrderResponseType = trape.datalayer.Enums.OrderResponseType;
using OrderSide = trape.datalayer.Enums.OrderSide;
using OrderType = trape.datalayer.Enums.OrderType;
using TimeInForce = trape.datalayer.Enums.TimeInForce;

namespace trape.cli.trader.Fees
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
        private ILogger _logger;

        /// <summary>
        /// Accountant
        /// </summary>
        private IAccountant _accountant;

        /// <summary>
        /// Buffer
        /// </summary>
        private IBuffer _buffer;

        /// <summary>
        /// Job to check available BNB fees
        /// </summary>
        private Job _jobFeeFundsChecker;

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Symbol for fees
        /// </summary>
        private readonly string _feeSymbol;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>FeeWatchdog</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="accountant">Accountant</param>
        /// <param name="buffer">Buffer</param>
        public FeeWatchdog(ILogger logger, IAccountant accountant, IBuffer buffer)
        {
            #region Arguments check

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            this._buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            this._logger = logger.ForContext(typeof(FeeWatchdog));
            this._cancellationTokenSource = new CancellationTokenSource();
            this._feeSymbol = "BNBUSDT";

            this._jobFeeFundsChecker = new Job(new TimeSpan(0, 5, 0), _feesChecker);
        }

        #endregion

        #region Timer

        private async void _feesChecker()
        {
            // Get remaining BNB balance for trading
            var bnb = await this._accountant.GetBalance(this._feeSymbol.Replace("USDT", string.Empty)).ConfigureAwait(true);
            if (bnb == null)
            {
                // Something is oddly wrong, wait a bit
                this._logger.Debug("Cannot retrieve account info");
                return;
            }

            var currentPrice = this._buffer.GetAskPrice(this._feeSymbol);

            // Threshold for buy is 53
            if (bnb.Free < 53)
            {
                this._logger.Information($"Fees NOT OK at {bnb.Free} - issuing buy");

                // Fixed for now, buy for 55 USDT
                var buy = 3;

                // Get merchant and place order
                var merchant = Program.Container.GetInstance<IStockExchange>();
                await merchant.PlaceOrder(new ClientOrder()
                {
                    Symbol = this._feeSymbol,
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

        #endregion

        #region Start / Stop

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            this._logger.Information("Starting Fee Watchdog");

            this._jobFeeFundsChecker.Start();

            this._logger.Information("Fee Watchdog started");
        }

        /// <summary>
        /// Terminate
        /// </summary>
        public void Terminate()
        {
            this._logger.Information("Fee Watchdog stopping");

            this._jobFeeFundsChecker.Terminate();

            this._cancellationTokenSource.Cancel();

            this._logger.Information("Fee Watchdog stopped");
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
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._accountant = null;
                this._buffer = null;
                this._cancellationTokenSource.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
