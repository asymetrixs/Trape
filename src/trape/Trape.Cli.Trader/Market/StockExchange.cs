namespace Trape.Cli.Trader.Market
{
    using Binance.Net.Objects.Spot.SpotData;
    using CryptoExchange.Net.Objects;
    using Serilog;
    using System;
    using System.Globalization;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using Trape.Cli.Trader.Cache;
    using Trape.Cli.Trader.Cache.Models;

    /// <summary>
    /// Stock exchange class
    /// </summary>
    public class StockExchange : IStockExchange, IDisposable
    {
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

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <c>StockExchange</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="cache">Cache</param>
        public StockExchange(ILogger logger, ICache cache)
        {
            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._cache = cache ?? throw new ArgumentNullException(paramName: nameof(cache));

            this._logger = logger.ForContext(typeof(StockExchange));
            this._newOrder = new Subject<BinancePlacedOrder>();
        }

        /// <summary>
        /// Informs about new orders
        /// </summary>
        public IObservable<BinancePlacedOrder> NewOrder => this._newOrder;

        /// <summary>
        /// Place the order at Binance
        /// </summary>
        /// <param name="clientOrder">Order</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns></returns>
        public async Task PlaceOrder(ClientOrder clientOrder, CancellationToken cancellationToken = default)
        {
            _ = clientOrder ?? throw new ArgumentNullException(paramName: nameof(clientOrder));

            // https://nsubstitute.github.io/help/getting-started/
            this._logger.Information($"{clientOrder.Symbol} @ {clientOrder.Price:0.00}: Issuing {clientOrder.Side.ToString().ToLower(CultureInfo.InvariantCulture)} of {clientOrder.Quantity}. Id: {clientOrder.Id}");

            // Block quantity until order is processed
            this._cache.AddOpenOrder(new OpenOrder(clientOrder.Id, clientOrder.Symbol, clientOrder.Quantity));

            WebCallResult<BinancePlacedOrder> placedOrder;

            // 2 second to complete
            using var cancellationTokenSource = new CancellationTokenSource(2000);
            cancellationTokenSource.Token.Register(() =>
            {
                this._cache.RemoveOpenOrder(clientOrder.Id);
                this._logger.Warning($"{clientOrder.Symbol}: Order timed out. Id: {clientOrder.Id}");
            });

            using var token = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken);

            this._logger.Information($"{clientOrder.Symbol}: {clientOrder.Id} Price: {clientOrder.Price} Quantity: {clientOrder.Quantity}");

            ////// Market does not require parameter 'timeInForce' and 'price'
            ////if (clientOrder.Type == OrderType.Market)
            ////{
            ////    placedOrder = await _binanceClient.Spot.Order.PlaceTestOrderAsync(
            ////        clientOrder.Symbol,
            ////        clientOrder.Side,
            ////        clientOrder.Type,
            ////        quantity: clientOrder.Quantity,
            ////        newClientOrderId: clientOrder.Id,
            ////        orderResponseType: clientOrder.OrderResponseType,
            ////        ct: token.Token).ConfigureAwait(true);
            ////}
            ////else
            ////{
            ////    placedOrder = await _binanceClient.Spot.Order.PlaceTestOrderAsync(
            ////        clientOrder.Symbol,
            ////        clientOrder.Side,
            ////        clientOrder.Type,
            ////        price: clientOrder.Price,
            ////        quantity: clientOrder.Quantity,
            ////        newClientOrderId: clientOrder.Id,
            ////        orderResponseType: clientOrder.OrderResponseType,
            ////        timeInForce: clientOrder.TimeInForce,
            ////        ct: token.Token).ConfigureAwait(true);
            ////}

            ////if (placedOrder.Success)
            ////{
            ////    _newOrder.OnNext(placedOrder.Data);
            ////    _logger.Debug($"{clientOrder.Symbol}: {clientOrder.Side} {clientOrder.Quantity} {clientOrder.Price:0.00} {clientOrder.Id}");
            ////}
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
                this._newOrder.OnCompleted();
                this._newOrder.Dispose();
            }

            this._disposed = true;
        }
    }
}
