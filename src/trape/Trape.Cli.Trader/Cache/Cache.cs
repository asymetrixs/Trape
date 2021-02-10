using Binance.Net.Objects.Spot.MarketData;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Trape.Cli.trader.Cache.Models;
using Trape.Cli.trader.Listener;

namespace Trape.Cli.Trader.Cache
{
    public class Cache : ICache
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
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Cache for open orders
        /// </summary>
        private readonly Dictionary<string, OpenOrder> _openOrders;


        /// <summary>
        /// Exchange Information
        /// </summary>
        public BinanceExchangeInfo BinanceExchangeInfo { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Cache"/> class.
        /// </summary>
        /// <param name="logger"></param>
        public Cache(ILogger logger, IListener listener)
        {
            #region Argument checks

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _listener = listener ?? throw new ArgumentNullException(nameof(listener));

            #endregion

            _disposed = false;
            _openOrders = new Dictionary<string, OpenOrder>();

            _listener.NewExchangeInfo.Subscribe((bei) =>
            {
                BinanceExchangeInfo = bei;
            });
        }

        #endregion

        /// <summary>
        /// Stores open orders
        /// </summary>
        /// <param name="openOrder">Open order</param>
        public void AddOpenOrder(OpenOrder openOrder)
        {
            #region Argument checks

            if (openOrder == null)
            {
                return;
            }

            #endregion

            if (_openOrders.ContainsKey(openOrder.Id))
            {
                _openOrders.Remove(openOrder.Id);
            }

            _logger.Debug($"Order added {openOrder.Id}");

            _openOrders.Add(openOrder.Id, openOrder);
        }

        /// <summary>
        /// Removes an open order
        /// </summary>
        /// <param name="clientOrderId">Id of open order</param>
        public void RemoveOpenOrder(string clientOrderId)
        {
            _logger.Debug($"Order removed {clientOrderId}");

            _openOrders.Remove(clientOrderId);
        }

        /// <summary>
        /// Returns the currently blocked
        /// </summary>
        public decimal GetOpenOrderValue(string symbol)
        {
            // Remove old orders
            foreach (var oo in _openOrders.Where(o => o.Value.CreatedOn < DateTime.UtcNow.AddSeconds(-10)).Select(o => o.Key))
            {
                _logger.Debug($"Order cleaned {oo}");
                _openOrders.Remove(oo);
            }

            return _openOrders.Where(o => o.Value.Symbol == symbol).Sum(o => o.Value.Quantity);
        }

        /// <summary>
        /// Indicates if an order for this symbol is open
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public bool HasOpenOrder(string symbol)
        {
            return _openOrders.Any(o => o.Value.Symbol == symbol);
        }


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

            }

            _disposed = true;
        }

        #endregion
    }
}
