using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.SpotData;
using CryptoExchange.Net.Objects;
using Serilog;
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Trape.Cli.Trader.Cache;
using Trape.Cli.Trader.Cache.Models;

namespace Trape.Cli.Trader.Market
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
        /// Cache
        /// </summary>
        private readonly ICache _cache;

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
        /// /// <param name="cache">Cache</param>
        /// <param name="binanceClient">Binance Client</param>
        public StockExchange(ILogger logger, ICache cache, IBinanceClient binanceClient)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _cache = cache ?? throw new ArgumentNullException(paramName: nameof(cache));

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
            _logger.Information($"{clientOrder.Symbol} @ {clientOrder.Price:0.00}: Issuing {clientOrder.Side.ToString().ToLower()} of {clientOrder.Quantity}. Id: {clientOrder.Id}");

            // Block quantity until order is processed
            _cache.AddOpenOrder(new OpenOrder(clientOrder.Id, clientOrder.Symbol, clientOrder.Quantity));

            WebCallResult<BinancePlacedOrder> placedOrder;

            // 2 second to complete
            using var cancellationTokenSource = new CancellationTokenSource(2000);
            cancellationTokenSource.Token.Register(() =>
            {
                _cache.RemoveOpenOrder(clientOrder.Id);
                _logger.Warning($"{clientOrder.Symbol}: Order timed out. Id: {clientOrder.Id}");
            });

            using var token = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken);

            //// Market does not require parameter 'timeInForce' and 'price'
            //if (clientOrder.Type == OrderType.Market)
            //{
            //    placedOrder = await _binanceClient.Spot.Order.PlaceTestOrderAsync(
            //        clientOrder.Symbol,
            //        clientOrder.Side,
            //        clientOrder.Type,
            //        quantity: clientOrder.Quantity,
            //        newClientOrderId: clientOrder.Id,
            //        orderResponseType: clientOrder.OrderResponseType,
            //        ct: token.Token).ConfigureAwait(true);
            //}
            //else
            //{
            //    placedOrder = await _binanceClient.Spot.Order.PlaceTestOrderAsync(
            //        clientOrder.Symbol,
            //        clientOrder.Side,
            //        clientOrder.Type,
            //        price: clientOrder.Price,
            //        quantity: clientOrder.Quantity,
            //        newClientOrderId: clientOrder.Id,
            //        orderResponseType: clientOrder.OrderResponseType,
            //        timeInForce: clientOrder.TimeInForce,
            //        ct: token.Token).ConfigureAwait(true);
            //}

            //if (placedOrder.Success)
            //{
            //    _newOrder.OnNext(placedOrder.Data);
            //    _logger.Debug($"{clientOrder.Symbol}: {clientOrder.Side} {clientOrder.Quantity} {clientOrder.Price:0.00} {clientOrder.Id}");
            //}
        }

        #endregion
    }
}
