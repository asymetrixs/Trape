using Binance.Net.Objects;
using Serilog;
using System;
using System.Threading;
using trape.cli.trader.Account;
using trape.cli.trader.Cache;
using trape.cli.trader.Market;
using trape.datalayer.Models;
using trape.jobs;

namespace trape.cli.trader.Fees
{
    /// <summary>
    /// Watchdog to always have sufficient BNBs available to pay fees
    /// </summary>
    public class FeeWatchdog : IFeeWatchdog
    {
        #region Fields

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

            _ = accountant ?? throw new ArgumentNullException(paramName: nameof(accountant));

            _ = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            this._logger = logger.ForContext(typeof(FeeWatchdog));
            this._accountant = accountant;
            this._buffer = buffer;
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
            var buyFor = 0M;
            if (bnb.Free < 53)
            {
                this._logger.Information($"Fees NOT OK at {bnb.Free} - issuing buy");

                // Fixed for now, buy for 55 USDT
                buyFor = 55;

                //// Get symbol info and run some checks
                //var symbolInfo = this._buffer.GetSymbolInfoFor("BNBUSDT");

                //if(symbolInfo != null)
                //{
                //    // Min notional
                //    buy = Math.Max(buy, symbolInfo.MinNotionalFilter.MinNotional);

                //    // Min price
                //    buy = Math.Max(buy, symbolInfo.PriceFilter.MinPrice);

                //    // Max price
                //    buy = Math.Min(buy, symbolInfo.PriceFilter.MaxPrice);
                //}

                // Get merchant and place order
                var merchant = Program.Container.GetInstance<IStockExchange>();
                await merchant.PlaceOrder(new ClientOrder()
                {
                    Symbol = this._feeSymbol,
                    Side = datalayer.Enums.OrderSide.Buy,
                    Type = datalayer.Enums.OrderType.Market,
                    QuoteOrderQuantity = buyFor,
                    Price = currentPrice
                }, this._cancellationTokenSource.Token).ConfigureAwait(true);

                this._logger.Information($"Issued buy of {buyFor} USDT");
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

        // TODO: Consistency: Stop / Finish / Terminate

        #endregion
    }
}
