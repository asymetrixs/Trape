using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Objects;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Account;
using trape.cli.trader.Analyze;
using trape.cli.trader.Cache;
using trape.cli.trader.DataLayer.Models;

namespace trape.cli.trader.trade
{
    public class Trader : ITrader
    {
        #region Fields

        private bool _disposed;

        private bool _tradingEnabled;

        private readonly ILogger _logger;

        private IAccountant _accountant;

        private IRecommender _recommender;

        private IBuffer _buffer;

        private System.Timers.Timer _timerTrading;

        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Constructor

        public Trader(ILogger logger, IAccountant accountant, IRecommender recommender, IBuffer buffer)
        {
            if (null == logger || null == accountant || null == recommender || null == buffer)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._accountant = accountant;
            this._recommender = recommender;
            this._buffer = buffer;
            this._tradingEnabled = true;
            this._cancellationTokenSource = new CancellationTokenSource();

            this._timerTrading = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 5).TotalMilliseconds
            };
            this._timerTrading.Elapsed += _timerTrading_Elapsed;
        }

        #endregion

        #region Timer Elapsed

        private async void _timerTrading_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var database = Pool.DatabasePool.Get();

            foreach (var symbol in this._buffer.GetSymbols())
            {
                // Get recommendation what to do
                var recommendation = this._recommender.GetRecommendation(symbol);
                if (null == recommendation || recommendation.Action == Analyze.Action.Wait)
                {
                    this._logger.Verbose($"{symbol}: Waiting for recommendation - Traded enabled: {this._tradingEnabled}");
                    continue;
                }

                // Get prices
                var bestAskPrice = this._buffer.GetAskPrice(symbol);
                var bestBidPrice = this._buffer.GetBidPrice(symbol);

                var assetBalance = await this._accountant.GetBalance(symbol.Replace("USDT", string.Empty)).ConfigureAwait(false);

                // Get remaining USDT balance for trading
                var usdt = await this._accountant.GetBalance("USDT").ConfigureAwait(false);

                var binanceClient = Program.Services.GetService(typeof(IBinanceClient)) as IBinanceClient;
                if (null == assetBalance || null == usdt)
                {
                    // Something is oddly wrong, wait a bit
                    this._logger.Debug("Cannot retrieve account info");
                    continue;
                }

                // Sell 66% of the asset
                var assetBalanceForSale = assetBalance?.Free;
                var sellQuoteOrderQuantity = assetBalanceForSale.HasValue ? assetBalanceForSale * bestBidPrice : null;

                // New client order id
                var newClientOrderId = Guid.NewGuid().ToString("N");

                // asking price, selling
                var askingFactor = 0.9999M;

                // bidding price, buying
                var bidFactor = 1.00015M;

                // Get last orders
                var lastOrders = await database.GetLastOrdersAsync(symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);
                var lastOrder = lastOrders.OrderByDescending(l => l.TransactionTime).FirstOrDefault();

                // Only take half of what is available to buy assets
                var availableAmount = usdt?.Free / 2;

                /*
                 Quote Order Qty Market orders have been enabled on all symbols.
                 Quote Order Qty MARKET orders allow a user to specify the total quoteOrderQty spent or received in the MARKET order.
                 Quote Order Qty MARKET orders will not break LOT_SIZE filter rules; the order will execute a quantity that will have the notional value as close as possible to quoteOrderQty.

                 Using BTCUSDT as an example:
                 On the BUY side, the order will buy as many BTC as quoteOrderQty USDT can.
                 On the SELL side, the order will sell as much BTC as needed to receive quoteOrderQty USDT.
                */
#if !DEBUG
                var canDo = this._accountant.GetAvailablePricesAndQuantities(symbol);
#endif

                var traded = false;
                WebCallResult<BinancePlacedOrder> placedOrder = null;

                if (this._tradingEnabled)
                {
                    // Buy
                    if (recommendation.Action == Analyze.Action.Buy)
                    {
                        // Calculate price to buy
                        var price = bestAskPrice * bidFactor;

                        // Check if no order has been issued yet or order was SELL
                        if ((null == lastOrder
                            || lastOrder.Side == OrderSide.Sell
                            || lastOrder.Side == OrderSide.Buy && lastOrder.Price * 0.99M > bestAskPrice && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow)
                            && availableAmount.HasValue
                            && bestAskPrice > 0)
                        {
                            placedOrder = await binanceClient.PlaceOrderAsync(symbol, OrderSide.Buy, OrderType.Limit,
                                quoteOrderQuantity: availableAmount, price: price, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                                timeInForce: TimeInForce.ImmediateOrCancel, ct: this._cancellationTokenSource.Token).ConfigureAwait(false);

                            await database.InsertAsync(new Order()
                            {
                                Symbol = symbol,
                                Side = OrderSide.Buy,
                                Type = OrderType.Limit,
                                QuoteOrderQuantity = availableAmount.Value,
                                Price = price,
                                NewClientOrderId = newClientOrderId,
                                OrderResponseType = OrderResponseType.Full,
                                TimeInForce = TimeInForce.ImmediateOrCancel
                            }, this._cancellationTokenSource.Token).ConfigureAwait(false);

                            this._logger.Debug($"symbol:{symbol};price:{price};quantity:{availableAmount}");

                            this._logger.Information($"Issued order to buy {symbol}");

                            traded = true;
                        }
                    }
                    // Sell
                    else if (recommendation.Action == Analyze.Action.Sell)
                    {
                        // Check if no order has been issued yet or order was BUY
                        if ((null == lastOrder
                            || lastOrder.Side == OrderSide.Buy
                            || lastOrder.Side == OrderSide.Sell && lastOrder.Price * 1.01M < bestBidPrice && lastOrder.TransactionTime.AddMinutes(15) < DateTime.UtcNow)
                            && sellQuoteOrderQuantity.HasValue && sellQuoteOrderQuantity > 0
                            /* implicit checking bestAskPrice > 0 by checking sellQuoteOrderQuantity > 0*/)
                        {
                            // Calculate price to sell
                            var price = bestBidPrice * askingFactor;

                            // Do not sell below buying price
                            // Select trades where we bought
                            // And where buying price is smaller than selling price
                            // And where asset is available
                            var availableQuantity = lastOrders.Where(l => l.Side == OrderSide.Buy && l.Price < price && l.Quantity > l.Consumed)
                                .Sum(l => (l.Quantity - l.Consumed));

                            // Sell what is maximal possible
                            sellQuoteOrderQuantity = sellQuoteOrderQuantity < availableQuantity ? sellQuoteOrderQuantity : availableQuantity;

                            placedOrder = await binanceClient.PlaceOrderAsync(symbol, OrderSide.Sell, OrderType.Limit,
                                quoteOrderQuantity: sellQuoteOrderQuantity, price: price, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                                timeInForce: TimeInForce.ImmediateOrCancel, ct: this._cancellationTokenSource.Token).ConfigureAwait(false);

                            await database.InsertAsync(new Order()
                            {
                                Symbol = symbol,
                                Side = OrderSide.Sell,
                                Type = OrderType.Limit,
                                QuoteOrderQuantity = sellQuoteOrderQuantity.Value,
                                Price = price,
                                NewClientOrderId = newClientOrderId,
                                OrderResponseType = OrderResponseType.Full,
                                TimeInForce = TimeInForce.ImmediateOrCancel
                            }, this._cancellationTokenSource.Token).ConfigureAwait(false);

                            this._logger.Debug($"symbol:{symbol};price:{price};quantity:{sellQuoteOrderQuantity}");

                            this._logger.Information($"Issued order to sell {symbol}");

                            traded = true;
                        }
                    }
                }


                this._logger.Debug($"recommendation:{recommendation.Action};traded:{traded};bestAskPrice:{bestAskPrice};bestBidPrice:{bestBidPrice};assetBalance:{assetBalance?.Asset};assetBalance.Free:{assetBalance?.Free};assetBalanceForSale:{assetBalanceForSale};" +
                    $"sellQuoteOrderQuantity:{sellQuoteOrderQuantity};newClientOrderId:{newClientOrderId};usdt?.Free:{usdt?.Free};availableAmount:{availableAmount}");
                if (traded)
                {
                    this._tradingEnabled = false;
                }
                
                this._logger.Debug($"Trading {symbol}: {this._tradingEnabled}");

                // Check if order placed
                if (null != placedOrder)
                {
                    // Check if order is OK and log
                    if (placedOrder.ResponseStatusCode != System.Net.HttpStatusCode.OK || !placedOrder.Success)
                    {
                        this._logger.Warning($"Order {newClientOrderId} was malformed.");
                        this._logger.Debug(placedOrder.Error?.Code.ToString());
                        this._logger.Debug(placedOrder.Error?.Message);
                        this._logger.Debug(placedOrder.Error?.Data.ToString());
                    }
                    else
                    {
                        await database.InsertAsync(placedOrder.Data, this._cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
            }

            Pool.DatabasePool.Put(database);
        }

        #endregion

        #region Start / Stop

        public void Start()
        {
            this._logger.Information("Starting Trader");

            this._timerTrading.Start();

            this._logger.Information("Trader started");
        }

        public async Task Stop()
        {
            this._logger.Information("Stopping Trader");

            this._timerTrading.Stop();

            // Give time for running task to end
            await Task.Delay(1000).ConfigureAwait(false);

            this._cancellationTokenSource.Cancel();

            this._logger.Information("Trader stopped");
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
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._timerTrading.Dispose();
            }

            this._disposed = true;
        }

        #endregion
    }
}
