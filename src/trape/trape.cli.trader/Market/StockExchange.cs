using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Objects;
using Serilog;
using Serilog.Context;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.DataLayer;
using trape.cli.trader.DataLayer.Models;

namespace trape.cli.trader.Market
{
    /// <summary>
    /// Stock exchange class
    /// </summary>
    class StockExchange : IStockExchange
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Binance Client
        /// </summary>
        private IBinanceClient _binanceClient;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>StockExchange</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="binanceClient">Binance Client</param>
        public StockExchange(ILogger logger, IBinanceClient binanceClient)
        {
            #region Argument checks

            if (logger == null)
            {
                throw new ArgumentNullException(paramName: nameof(logger));
            }

            if (binanceClient == null)
            {
                throw new ArgumentNullException(paramName: nameof(binanceClient));
            }

            #endregion

            this._logger = logger.ForContext(typeof(StockExchange));
            this._binanceClient = binanceClient;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Place the order at Binance
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="orderSide">Side</param>
        /// <param name="orderType">Type</param>
        /// <param name="quoteOrderQuantity">Quantity in the currency the Asset is traded against, e.g. BTCUSDT -> USDT</param>
        /// <param name="price">Current Price of one entity</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public async Task PlaceOrder(string symbol, OrderSide orderSide, OrderType orderType, decimal quoteOrderQuantity, decimal price,
            CancellationToken cancellationToken)
        {
            var database = Pool.DatabasePool.Get();

            try
            {
                var newClientOrderId = Guid.NewGuid().ToString("N");

                // Place the order
                var placedOrder = await this._binanceClient.PlaceOrderAsync(symbol, orderSide, orderType,
                    quoteOrderQuantity, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                    ct: cancellationToken).ConfigureAwait(true);

                // Log order in custom format
                await database.InsertAsync(new Order()
                {
                    Symbol = symbol,
                    Side = OrderSide.Sell,
                    Type = OrderType.Limit,
                    QuoteOrderQuantity = quoteOrderQuantity,
                    Price = price,
                    NewClientOrderId = newClientOrderId,
                    OrderResponseType = OrderResponseType.Full,
                    TimeInForce = TimeInForce.ImmediateOrCancel
                }, cancellationToken).ConfigureAwait(true);

                await LogOrder(database, newClientOrderId, placedOrder, cancellationToken);
            }
            catch (Exception e)
            {
                this._logger.Error(e.Message, e);
            }
            finally
            {
                Pool.DatabasePool.Put(database);
            }
        }

        /// <summary>
        /// Logs an order in the database
        /// </summary>
        /// <param name="database">Database</param>
        /// <param name="newClientOrderId">Generated new client order id</param>
        /// <param name="placedOrder">Placed order or null if none</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        private async Task LogOrder(ITrapeContext database, string newClientOrderId, WebCallResult<BinancePlacedOrder> placedOrder, CancellationToken cancellationToken)
        {
            #region Argument checks

            if (database == null)
            {
                throw new ArgumentNullException(paramName: nameof(database));
            }

            if (string.IsNullOrEmpty(newClientOrderId))
            {
                throw new ArgumentNullException(paramName: nameof(newClientOrderId));
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException(paramName: nameof(cancellationToken));
            }

            #endregion

            // Check if order placed
            if (placedOrder != null)
            {
                // Check if order is OK and log
                if (placedOrder.ResponseStatusCode != System.Net.HttpStatusCode.OK || !placedOrder.Success)
                {
                    using (var context1 = LogContext.PushProperty("placedOrder.Error", placedOrder.Error))
                    using (var context2 = LogContext.PushProperty("placedOrder.Data", placedOrder.Data))
                    {
                        this._logger.Error($"Order {newClientOrderId} was malformed");
                    }

                    // Logging
                    this._logger.Error(placedOrder.Error?.Code.ToString());
                    this._logger.Error(placedOrder.Error?.Message);
                    this._logger.Error(placedOrder.Error?.Data?.ToString());

                    if (placedOrder.Data != null)
                    {
                        this._logger.Warning($"PlacedOrder: {placedOrder.Data.Symbol};{placedOrder.Data.Side};{placedOrder.Data.Type} > ClientOrderId:{placedOrder.Data.ClientOrderId} CummulativeQuoteQuantity:{placedOrder.Data.CummulativeQuoteQuantity} OriginalQuoteOrderQuantity:{placedOrder.Data.OriginalQuoteOrderQuantity} Status:{placedOrder.Data.Status}");
                    }
                }
                else
                {
                    await database.InsertAsync(placedOrder.Data, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        #endregion
    }
}
