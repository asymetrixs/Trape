using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Objects;
using Serilog;
using System;
using System.Collections.Concurrent;
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

        private ILogger _logger;

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
            this._cancellationTokenSource = new CancellationTokenSource();

            this._timerTrading = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 1).TotalMilliseconds
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
                if (null == recommendation)
                {
                    continue;
                }


                // Get prices
                var bestAskPrice = this._buffer.GetAskPrice(symbol);
                var bestBidPrice = this._buffer.GetBidPrice(symbol);

                var assetBalance = this._accountant.GetBalance(symbol.Replace("USDT", string.Empty));

                // Sell 66% of the asset
                var assetBalanceForSale = assetBalance.Free * 0.65M;

                var sellQuoteOrderQuantity = assetBalanceForSale * bestAskPrice;

                var newClientOrderId = Guid.NewGuid().ToString("N");
                var binanceClient = Program.Services.GetService(typeof(IBinanceClient)) as IBinanceClient;

                // asking price, selling
                var askingFactor = 0.9998M;

                // bidding price, buying
                var bidFactor = 1.00015M;

                var lastOrder = await database.GetLastOrderAsync(symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);

                // Get remaining USDT balance for trading
                var usdt = this._accountant.GetBalance("USDT");

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

                throw new NotImplementedException("Get amount that can be traded from accountant");

                WebCallResult<BinancePlacedOrder> placedOrder = null;
                // Buy
                if (recommendation.Action == Analyze.Action.Buy)
                {
                    // Calculate price to buy
                    var price = bestBidPrice * bidFactor;

                    // Check if no order has been issued yet or order was SELL
                    if ((null == lastOrder || lastOrder.Side == OrderSide.Sell
                        || lastOrder.Side == OrderSide.Buy && lastOrder.Price > bestBidPrice && lastOrder.EventTime.AddMinutes(15) < DateTime.UtcNow)
                        && availableAmount.HasValue)
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

                        this._logger.Information($"Issued order to buy {symbol} ");

                    }
                }
                // Sell
                else if (recommendation.Action == Analyze.Action.Sell)
                {
                    // Check if no order has been issued yet or order was BUY
                    if ((null == lastOrder || lastOrder.Side == OrderSide.Buy
                        || lastOrder.Side == OrderSide.Sell && lastOrder.Price < bestAskPrice && lastOrder.EventTime.AddMinutes(15) < DateTime.UtcNow)
                        && sellQuoteOrderQuantity > 0)
                    {
                        // Calculate price to sell
                        var price = bestAskPrice * askingFactor;

                        if ((null == lastOrder))

                            placedOrder = await binanceClient.PlaceOrderAsync(symbol, OrderSide.Sell, OrderType.Limit,
                                quoteOrderQuantity: sellQuoteOrderQuantity, price: price, newClientOrderId: newClientOrderId, orderResponseType: OrderResponseType.Full,
                                timeInForce: TimeInForce.ImmediateOrCancel, ct: this._cancellationTokenSource.Token).ConfigureAwait(false);

                        await database.InsertAsync(new Order()
                        {
                            Symbol = symbol,
                            Side = OrderSide.Sell,
                            Type = OrderType.Limit,
                            QuoteOrderQuantity = sellQuoteOrderQuantity,
                            Price = price,
                            NewClientOrderId = newClientOrderId,
                            OrderResponseType = OrderResponseType.Full,
                            TimeInForce = TimeInForce.ImmediateOrCancel
                        }, this._cancellationTokenSource.Token).ConfigureAwait(false);

                        this._logger.Information($"Issued order to sell {symbol} ");
                    }
                }
                // Nothing
                else
                {
                    this._logger.Information($"Waiting for recommendation {symbol} ");
                }

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

        public async Task Start()
        {
            this._logger.Information("Starting Trader");

            throw new NotImplementedException();



            this._timerTrading.Start();

            this._logger.Information("Trader started");
        }

        public void Stop()
        {
            this._logger.Information("Stopping Trader");

            this._timerTrading.Stop();

            throw new NotImplementedException();

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
