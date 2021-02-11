namespace Trape.Cli.Trader.Cache
{
    using Binance.Net.Objects.Spot.MarketData;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Trape.Cli.Trader.Cache.Models;
    using Trape.Cli.Trader.Listener;

    public class Store : IStore, IDisposable
    {
        /// <summary>
        /// Cache for open orders
        /// </summary>
        private readonly Dictionary<string, OpenOrder> _openOrders;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Listener
        /// </summary>
        private readonly IListener _listener;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Store"/> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="listener">Listener</param>
        public Store(ILogger logger, IListener listener)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this._listener = listener ?? throw new ArgumentNullException(nameof(listener));

            this._disposed = false;
            this._openOrders = new Dictionary<string, OpenOrder>();
            this.BinanceExchangeInfo = null;

            _ = this._listener.NewExchangeInfo.Subscribe((bei) => this.BinanceExchangeInfo = bei);
        }

        /// <summary>
        /// Exchange Information
        /// </summary>
        public BinanceExchangeInfo? BinanceExchangeInfo { get; private set; }

        /// <summary>
        /// Stores open orders
        /// </summary>
        /// <param name="openOrder">Open order</param>
        public void AddOpenOrder(OpenOrder openOrder)
        {
            if (openOrder == null)
            {
                return;
            }

            if (this._openOrders.ContainsKey(openOrder.Id))
            {
                this._openOrders.Remove(openOrder.Id);
            }

            this._logger.Debug($"Order added {openOrder.Id}");

            this._openOrders.Add(openOrder.Id, openOrder);
        }

        /// <summary>
        /// Removes an open order
        /// </summary>
        /// <param name="clientOrderId">Id of open order</param>
        public void RemoveOpenOrder(string clientOrderId)
        {
            this._logger.Debug($"Order removed {clientOrderId}");

            this._openOrders.Remove(clientOrderId);
        }

        /// <summary>
        /// Returns the currently blocked
        /// </summary>
        public decimal GetOpenOrderValue(string symbol)
        {
            // Remove old orders
            foreach (var oo in this._openOrders.Where(o => o.Value.CreatedOn < DateTime.UtcNow.AddSeconds(-10)).Select(o => o.Key))
            {
                this._logger.Debug($"Order cleaned {oo}");
                this._openOrders.Remove(oo);
            }

            return this._openOrders.Where(o => o.Value.Symbol == symbol).Sum(o => o.Value.Quantity);
        }

        /// <summary>
        /// Indicates if an order for this symbol is open
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public bool HasOpenOrder(string symbol)
        {
            return this._openOrders.Any(o => o.Value.Symbol == symbol);
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
            }

            this._disposed = true;
        }
    }
}
