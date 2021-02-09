using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.SpotData;
using CryptoExchange.Net.Objects;
using Serilog;
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.trader.Listener;
using Trape.Cli.trader.Listener.Models;
using Trape.Datalayer.Models;

namespace Trape.Cli.trader.Market
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
        /// Listener
        /// </summary>
        private readonly IListener _listener;

        /// <summary>
        /// Binance Client
        /// </summary>
        private readonly IBinanceClient _binanceClient;

        /// <summary>
        /// Orders
        /// </summary>
        private readonly Subject<BinancePlacedOrder> _newOrder;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>StockExchange</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// /// <param name="buffer">Buffer</param>
        /// <param name="binanceClient">Binance Client</param>
        public StockExchange(ILogger logger, IListener buffer, IBinanceClient binanceClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _listener = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            _binanceClient = binanceClient ?? throw new ArgumentNullException(paramName: nameof(binanceClient));

            #endregion

            _logger = logger.ForContext(typeof(StockExchange));
            _newOrder = new Subject<BinancePlacedOrder>();
        }

        #endregion

        /// <summary>
        /// Informs about new orders
        /// </summary>
        public IObservable<BinancePlacedOrder> NewOrder => _newOrder;

        #region Methods

        /// <summary>
        /// Place the order at Binance
        /// </summary>
        /// <param name="clientOrder">Order</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public async Task PlaceOrder(ClientOrder clientOrder, CancellationToken cancellationToken = default)
        {
            #region Argument checks

            _ = clientOrder ?? throw new ArgumentNullException(paramName: nameof(clientOrder));

            #endregion
            // https://nsubstitute.github.io/help/getting-started/
            _logger.Information($"{clientOrder.Symbol} @ {clientOrder.Price:0.00}: Issuing {clientOrder.Side.ToString().ToLower()} of {clientOrder.Quantity}");

            // Block quantity until order is processed
            _listener.AddOpenOrder(new OpenOrder(clientOrder.Id, clientOrder.Symbol, clientOrder.Quantity));

            // Place the order with binance tools
            var binanceSide = (OrderSide)(int)clientOrder.Side;
            var binanceType = (OrderType)(int)clientOrder.Type;
            var binanceResponseType = (OrderResponseType)(int)clientOrder.OrderResponseType;
            var timeInForce = (TimeInForce)(int)clientOrder.TimeInForce;

            WebCallResult<BinancePlacedOrder> placedOrder;

            // Market does not require parameter 'timeInForce' and 'price'
            if (binanceType == OrderType.Market)
            {
                placedOrder = await _binanceClient.Spot.Order.PlaceTestOrderAsync(
                    clientOrder.Symbol,
                    binanceSide,
                    binanceType,
                    quantity: clientOrder.Quantity,
                    newClientOrderId: clientOrder.Id,
                    orderResponseType: binanceResponseType,
                    ct: cancellationToken).ConfigureAwait(true);
            }
            else
            {
                placedOrder = await _binanceClient.Spot.Order.PlaceTestOrderAsync(
                    clientOrder.Symbol,
                    binanceSide,
                    binanceType,
                    price: clientOrder.Price,
                    quantity: clientOrder.Quantity,
                    newClientOrderId: clientOrder.Id,
                    orderResponseType: binanceResponseType,
                    timeInForce: timeInForce,
                    ct: cancellationToken).ConfigureAwait(true);
            }

            if (placedOrder.Success)
            {
                _newOrder.OnNext(placedOrder.Data);
            }

            _logger.Debug($"{clientOrder.Symbol}: {clientOrder.Side} {clientOrder.Quantity} {clientOrder.Price:0.00} {clientOrder.Id}");
        }

        #endregion
    }
}
