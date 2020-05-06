﻿using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Objects;
using Serilog;
using Serilog.Context;
using System;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache;
using trape.cli.trader.DataLayer;
using trape.cli.trader.DataLayer.Models;

namespace trape.cli.trader.Market
{
    /// <summary>
    /// Stock exchange class
    /// </summary>
    public class StockExchange : IStockExchange
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Buffer
        /// </summary>
        private IBuffer _buffer;

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
        /// /// <param name="buffer">Buffer</param>
        /// <param name="binanceClient">Binance Client</param>
        public StockExchange(ILogger logger, IBuffer buffer, IBinanceClient binanceClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _ = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            #endregion

            this._logger = logger.ForContext(typeof(StockExchange));
            this._buffer = buffer;
            this._binanceClient = binanceClient;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Place the order at Binance
        /// </summary>
        /// <param name="Order">Order</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public async Task PlaceOrder(Order order, CancellationToken cancellationToken = default)
        {
            #region Argument checks

            _ = order ?? throw new ArgumentNullException(paramName: nameof(order));

            #endregion
            // https://nsubstitute.github.io/help/getting-started/
            var database = Pool.DatabasePool.Get();

            try
            {
                // Place the order
                var placedOrder = await this._binanceClient.PlaceOrderAsync(order.Symbol, order.Side, order.Type,
                    quoteOrderQuantity: order.QuoteOrderQuantity, newClientOrderId: order.NewClientOrderId, orderResponseType: OrderResponseType.Full,
                    ct: cancellationToken).ConfigureAwait(true);

                // Log order in custom format
                await database.InsertAsync(order, cancellationToken).ConfigureAwait(true);

                await LogOrder(database, order.NewClientOrderId, placedOrder, cancellationToken).ConfigureAwait(true);
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
        private async Task LogOrder(ITrapeContext database, string newClientOrderId, WebCallResult<BinancePlacedOrder> placedOrder, CancellationToken cancellationToken = default)
        {
            #region Argument checks

            _ = database ?? throw new ArgumentNullException(paramName: nameof(database));

            if (string.IsNullOrEmpty(newClientOrderId))
            {
                throw new ArgumentNullException(paramName: nameof(newClientOrderId));
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
                        this._logger.Error($"Order {newClientOrderId} caused an error");
                    }

                    // Error Codes: https://github.com/binance-exchange/binance-official-api-docs/blob/master/errors.md

                    // Logging
                    this._logger.Error($"{placedOrder.Error?.Code.ToString()}: {placedOrder.Error?.Message}");
                    this._logger.Error(placedOrder.Error?.Data?.ToString());

                    if (placedOrder.Data != null)
                    {
                        this._logger.Warning($"PlacedOrder: {placedOrder.Data.Symbol};{placedOrder.Data.Side};{placedOrder.Data.Type} > ClientOrderId:{placedOrder.Data.ClientOrderId} CummulativeQuoteQuantity:{placedOrder.Data.CummulativeQuoteQuantity} OriginalQuoteOrderQuantity:{placedOrder.Data.OriginalQuoteOrderQuantity} Status:{placedOrder.Data.Status}");
                    }

                    // TODO: 1015 - TOO_MANY_ORDERS
                }
                else
                {
                    await database.InsertAsync(placedOrder.Data, cancellationToken).ConfigureAwait(true);
                    this._buffer.AddOpenOrder(new Cache.Models.OpenOrder(placedOrder.Data.ClientOrderId, placedOrder.Data.Symbol, placedOrder.Data.OriginalQuoteOrderQuantity));
                }
            }
        }

        #endregion
    }
}
