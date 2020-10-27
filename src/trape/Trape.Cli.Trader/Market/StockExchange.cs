using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.SpotData;
using CryptoExchange.Net.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using SimpleInjector.Lifestyles;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache;
using trape.cli.trader.Cache.Models;
using trape.datalayer;
using trape.datalayer.Models;
using trape.mapper;

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
        private readonly ILogger _logger;

        /// <summary>
        /// Buffer
        /// </summary>
        private readonly IBuffer _buffer;

        /// <summary>
        /// Binance Client
        /// </summary>
        private readonly IBinanceClient _binanceClient;

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

            _buffer = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            #endregion

            _logger = logger.ForContext(typeof(StockExchange));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Place the order at Binance
        /// </summary>
        /// <param name="Order">Order</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public async Task PlaceOrder(ClientOrder clientOrder, CancellationToken cancellationToken = default)
        {
            #region Argument checks

            _ = clientOrder ?? throw new ArgumentNullException(paramName: nameof(clientOrder));

            #endregion
            // https://nsubstitute.github.io/help/getting-started/
            using (AsyncScopedLifestyle.BeginScope(Program.Container))
            {
                _logger.Information($"{clientOrder.Symbol} @ {clientOrder.Price:0.00}: Issuing {clientOrder.Side.ToString().ToLower()} of {clientOrder.Quantity}");

                var database = Program.Container.GetService<TrapeContext>();

                try
                {
                    // Block quantity until order is processed
                    _buffer.AddOpenOrder(new OpenOrder(clientOrder.Id, clientOrder.Symbol, clientOrder.Quantity));

                    // Place the order with binance tools
                    var binanceSide = (OrderSide)(int)clientOrder.Side;
                    var binanceType = (OrderType)(int)clientOrder.Type;
                    var binanceResponseType = (OrderResponseType)(int)clientOrder.OrderResponseType;
                    var timeInForce = (TimeInForce)(int)clientOrder.TimeInForce;

                    WebCallResult<BinancePlacedOrder> placedOrder;

                    // Market does not require parameter 'timeInForce' and 'price'
                    if (binanceType == OrderType.Market)
                    {
                        placedOrder = await _binanceClient.Spot.Order.PlaceTestOrderAsync(clientOrder.Symbol, binanceSide, binanceType,
                          quantity: clientOrder.Quantity, newClientOrderId: clientOrder.Id, orderResponseType: binanceResponseType,
                          ct: cancellationToken).ConfigureAwait(true);
                    }
                    else
                    {
                        placedOrder = await _binanceClient.Spot.Order.PlaceTestOrderAsync(clientOrder.Symbol, binanceSide, binanceType, price: clientOrder.Price,
                          quantity: clientOrder.Quantity, newClientOrderId: clientOrder.Id, orderResponseType: binanceResponseType, timeInForce: timeInForce,
                          ct: cancellationToken).ConfigureAwait(true);
                    }

                    var attempts = 3;
                    while (attempts > 0)
                    {
                        try
                        {
                            // Log order in custom format
                            // Due to timing from Binance this might be executed after updates occurred
                            // So check if it exists in the database and update, otherwise add new
                            _logger.Information($"Loading existing Client Order: {clientOrder.Id}");
                            var existingClientOrder = database.ClientOrder.FirstOrDefault(c => c.Id == clientOrder.Id);

                            _logger.Information($"Client Order found: {existingClientOrder != null} : {clientOrder.Id}");

                            if (existingClientOrder == null)
                            {
                                database.ClientOrder.Add(clientOrder);

                                _logger.Information($"Adding new Client Order: {clientOrder.Id}");
                            }
                            else
                            {
                                existingClientOrder.CreatedOn = clientOrder.CreatedOn;
                                existingClientOrder.Order = clientOrder.Order;
                                existingClientOrder.OrderResponseType = clientOrder.OrderResponseType;
                                existingClientOrder.Price = clientOrder.Price;
                                existingClientOrder.Quantity = clientOrder.Quantity;
                                existingClientOrder.Side = clientOrder.Side;
                                existingClientOrder.Symbol = clientOrder.Symbol;
                                existingClientOrder.TimeInForce = clientOrder.TimeInForce;
                                existingClientOrder.Type = clientOrder.Type;

                                _logger.Information($"Updating existing Client Order: {clientOrder.Id}");
                            }

                            _logger.Debug($"{clientOrder.Symbol}: {clientOrder.Side} {clientOrder.Quantity} {clientOrder.Price:0.00} {clientOrder.Id}");

                            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

                            await LogOrder(database, clientOrder.Id, placedOrder, cancellationToken).ConfigureAwait(true);

                            break;
                        }
                        catch (Exception coe)
                        {
                            attempts--;

                            _logger.Information($"Failed attempt to store Client Order {clientOrder.Id}; attempt: {attempts}");

                            database.Entry(clientOrder).State = EntityState.Modified;

                            try
                            {
                                await database.SaveChangesAsync().ConfigureAwait(true);
                                _logger.Warning($"Modified state accepted for: {clientOrder.Id}");
                            }
                            catch (Exception coe2)
                            {
                                _logger.Error(coe, $"Finally attempt to store Client Order {clientOrder.Id} failed");

                                _logger.Warning(coe2, $"Modified state not accepted for: {clientOrder.Id}");
                            }

                            if (attempts == 0)
                            {
                                throw;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                }
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
        private async Task LogOrder(TrapeContext database, string newClientOrderId, WebCallResult<BinancePlacedOrder> placedOrder, CancellationToken cancellationToken = default)
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
                        _logger.Error($"Order {newClientOrderId} caused an error");
                    }

                    // Error Codes: https://github.com/binance-exchange/binance-official-api-docs/blob/master/errors.md

                    // Logging
                    _logger.Error($"{placedOrder.Error?.Code.ToString()}: {placedOrder.Error?.Message}");
                    _logger.Error(placedOrder.Error?.Data?.ToString());

                    if (placedOrder.Data != null)
                    {
                        _logger.Warning($"PlacedOrder: {placedOrder.Data.Symbol};{placedOrder.Data.Side};{placedOrder.Data.Type} > ClientOrderId:{placedOrder.Data.ClientOrderId} QuoteQuantity:{placedOrder.Data.QuoteQuantity} QuoteQuantityFilled:{placedOrder.Data.QuoteQuantityFilled} Status:{placedOrder.Data.Status}");
                    }

                    // TODO: 1015 - TOO_MANY_ORDERS
                }
                else
                {
                    var attempts = 3;
                    while (attempts > 0)
                    {
                        try
                        {
                            var newPlacedOrder = Translator.Translate(placedOrder.Data);
                            var existingPlacedOrder = database.PlacedOrders.FirstOrDefault(p => p.OrderId == newPlacedOrder.OrderId);

                            if (existingPlacedOrder == null)
                            {
                                await database.PlacedOrders.AddAsync(newPlacedOrder).ConfigureAwait(true);
                            }
                            else
                            {
                                existingPlacedOrder.ClientOrderId = newPlacedOrder.ClientOrderId;
                                existingPlacedOrder.QuantityFilled = newPlacedOrder.QuoteQuantity;
                                existingPlacedOrder.QuoteQuantityFilled = newPlacedOrder.QuoteQuantityFilled;
                                existingPlacedOrder.MarginBuyBorrowAmount = newPlacedOrder.MarginBuyBorrowAmount;
                                existingPlacedOrder.MarginBuyBorrowAsset = newPlacedOrder.MarginBuyBorrowAsset;
                                existingPlacedOrder.OrderListId = newPlacedOrder.OrderListId;
                                existingPlacedOrder.OriginalClientOrderId = newPlacedOrder.OriginalClientOrderId;
                                existingPlacedOrder.Quantity = newPlacedOrder.Quantity;
                                existingPlacedOrder.QuoteQuantity = newPlacedOrder.QuoteQuantity;
                                existingPlacedOrder.Price = newPlacedOrder.Price;
                                existingPlacedOrder.Side = newPlacedOrder.Side;
                                existingPlacedOrder.Status = newPlacedOrder.Status;
                                existingPlacedOrder.StopPrice = newPlacedOrder.StopPrice;
                                existingPlacedOrder.Symbol = newPlacedOrder.Symbol;
                                existingPlacedOrder.TimeInForce = newPlacedOrder.TimeInForce;
                                existingPlacedOrder.TransactionTime = newPlacedOrder.TransactionTime;
                                existingPlacedOrder.Type = newPlacedOrder.Type;
                            }

                            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

                            break;
                        }
                        catch
                        {
                            attempts--;

                            Log.Information($"Failed attempt to store Placed Order {newClientOrderId}; attempt: {attempts}");

                            if (attempts == 0)
                            {
                                throw;
                            }
                        }
                    }

                    _logger.Information($"{placedOrder.Data.Symbol} @ {placedOrder.Data.Price:0.00}: Issuing sell of {placedOrder.Data.QuantityFilled} / {placedOrder.Data.Quantity}");
                }
            }
        }

        #endregion
    }
}
