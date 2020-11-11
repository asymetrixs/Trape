using Serilog;
using System;
using System.Threading;
using Trape.Cli.trader.Account;
using Trape.Cli.trader.Cache;
using Trape.Cli.trader.Market;
using Trape.Datalayer.Models;
using Trape.Jobs;
using OrderResponseType = Trape.Datalayer.Enums.OrderResponseType;
using OrderSide = Trape.Datalayer.Enums.OrderSide;
using OrderType = Trape.Datalayer.Enums.OrderType;
using TimeInForce = Trape.Datalayer.Enums.TimeInForce;

namespace Trape.Cli.trader.Fees
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
        private IAccountant _accountant;

        /// <summary>
        /// Buffer
        /// </summary>
        private IBuffer _buffer;

        /// <summary>
        /// Job to check available BNB fees
        /// </summary>
        private readonly Job _jobFeeFundsChecker;

        /// <summary>
        /// Cancellation Token Source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

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

            _accountant = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            _buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            _logger = logger.ForContext(typeof(FeeWatchdog));
            _cancellationTokenSource = new CancellationTokenSource();
            _feeSymbol = "BNBUSDT";

            _jobFeeFundsChecker = new Job(new TimeSpan(0, 5, 0), _feesChecker);
        }

        #endregion

        #region Timer

        private async void _feesChecker()
        {
            // Get remaining BNB balance for trading
            var bnb = await _accountant.GetBalance(_feeSymbol.Replace("USDT", string.Empty)).ConfigureAwait(true);
            if (bnb == null)
            {
                // Something is oddly wrong, wait a bit
                _logger.Debug("Cannot retrieve account info");
                return;
            }

            var currentPrice = _buffer.GetAskPrice(_feeSymbol);

            // Threshold for buy is 53
            if (bnb.Free < 53)
            {
                _logger.Information($"Fees NOT OK at {bnb.Free} - issuing buy");

                // Fixed for now, buy for 55 USDT
                var buy = 3;

                // Get merchant and place order
                var merchant = Program.Container.GetInstance<IStockExchange>();
                await merchant.PlaceOrder(new ClientOrder()
                {
                    Symbol = _feeSymbol,
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

            _jobFeeFundsChecker.Start();

            _logger.Information("Fee Watchdog started");
        }

        /// <summary>
        /// Terminate
        /// </summary>
        public void Terminate()
        {
            _logger.Information("Fee Watchdog stopping");

            _jobFeeFundsChecker.Terminate();

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
                _accountant = null;
                _buffer = null;
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
