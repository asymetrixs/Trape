using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using trape.cli.trader.Cache.Models;
using trape.cli.trader.DataLayer;

namespace trape.cli.trader
{
    public class DecisionMaker : IDisposable
    {
        private ILogger _logger;

        private Cache.Buffer _buffer;

        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        private Timer _makeDecision;

        private Dictionary<string, Decision> _rates;

        private bool _disposed;

        public DecisionMaker(ILogger logger, Cache.Buffer buffer)
        {
            if (null == logger || null == buffer)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._buffer = buffer;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._rates = new Dictionary<string, Decision>();
            this._disposed = false;

            this._makeDecision = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 5).TotalMilliseconds
            };
            this._makeDecision.Elapsed += _makeDecision_Elapsed;
        }

        private async void _makeDecision_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Program.Services.GetService(typeof(ITrapeContext)) as ITrapeContext;

            foreach (var symbol in this._buffer.GetSymbols())
            {
                var t3s = this._buffer.Trends3Seconds;
                var t15s = this._buffer.Trends15Seconds;
                var t2m = this._buffer.Trends2Minutes;
                var t10m = this._buffer.Trends10Minutes;
                var t2h = this._buffer.Trends2Hours;
                var cp = this._buffer.CurrentPrices;

                var trend3Seconds = t3s.SingleOrDefault(t => t.Symbol == symbol);
                var trend15Seconds = t15s.SingleOrDefault(t => t.Symbol == symbol);
                var trend2Minutes = t2m.SingleOrDefault(t => t.Symbol == symbol);
                var trend10Minutes = t10m.SingleOrDefault(t => t.Symbol == symbol);
                var trend2Hours = t2h.SingleOrDefault(t => t.Symbol == symbol);
                var currentPrice = cp.SingleOrDefault(c => c.Symbol == symbol);

                if (null == trend3Seconds
                    || null == trend15Seconds
                    || null == trend2Minutes
                    || null == trend10Minutes
                    || null == trend2Hours
                    || null == currentPrice || currentPrice.EventTime < DateTime.UtcNow.AddSeconds(-3))
                {
                    this._logger.Warning($"Skipped {symbol} due to old or incomplete data");
                    continue;
                }

                var lastDecision = this._rates.FirstOrDefault(d => d.Key == symbol);

                var price = await database.GetCurrentPrice(symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);

                // No trade yet
                if (!this._rates.ContainsKey(symbol))
                {
                    if (trend10Minutes.Hours2 < 0 && trend2Minutes.Minutes10 > 0)
                    {
                        this._rates.Add(symbol, new Decision()
                        {
                            Action = "Buy",
                            Price = price,
                            Symbol = symbol
                        });

                        this._logger.Verbose($"A {symbol} @ {Math.Round(price, 6).ToString("0000.000000")}: {_GetTrend(trend10Minutes, trend2Minutes, trend3Seconds)}");

                        await database.Insert(lastDecision.Value, trend3Seconds, trend15Seconds, trend2Minutes, trend10Minutes, trend2Hours, this._cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        this._logger.Verbose($"P {symbol} @ {Math.Round(price, 6).ToString("0000.000000")}: {_GetTrend(trend10Minutes, trend2Minutes, trend3Seconds)}");
                    }
                }
                else if (lastDecision.Value.Action == "Buy"
                    && price * 1.01M > lastDecision.Value.Price
                    && trend10Minutes.IsValid() && trend10Minutes.Hours2 > 0
                    && trend10Minutes.IsValid() && trend10Minutes.Hours1 > 0
                    && trend2Minutes.IsValid() && trend2Minutes.Minutes15 < 0
                    && trend2Minutes.IsValid() && trend2Minutes.Minutes10 < 0
                    && trend15Seconds.IsValid() && trend15Seconds.Minutes3 > 0)
                {
                    this._logger.Information($"S {symbol} @ {Math.Round(price, 6).ToString("0000.000000")}: {_GetTrend(trend10Minutes, trend2Minutes, trend3Seconds)}");

                    lastDecision = new KeyValuePair<string, Decision>(symbol, new Decision()
                    {
                        Action = "Sell",
                        Price = price,
                        Symbol = symbol
                    });

                    if (this._rates.ContainsKey(symbol))
                    {
                        this._rates.Remove(symbol);
                    }

                    var socketClient = new Binance.Net.BinanceClient(new Binance.Net.Objects.BinanceClientOptions()
                    {
                        ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(Configuration.GetValue("binance:apikey"),
                                                       Configuration.GetValue("binance:secretkey")),
                        RateLimitingBehaviour = CryptoExchange.Net.Objects.RateLimitingBehaviour.Fail
                    });
                    var ai = socketClient.GetAccountInfo();
                        
                    var @as = socketClient.GetAccountStatus();
                    socketClient.PlaceMarginOrder("BTCUSDT", Binance.Net.Objects.OrderSide.Buy, Binance.Net.Objects.OrderType.TakeProfitLimit,
                        quantity: 0.5, newClientOrderId: System.Guid.NewGuid(), timeInForce: Binance.Net.Objects.TimeInForce.GoodTillCancel,
                        sideEffectType: Binance.Net.Objects.SideEffectType.MarginBuy, orderResponseType: Binance.Net.Objects.OrderResponseType.Full



                    this._rates.Add(lastDecision.Key, lastDecision.Value);
                    await database.Insert(lastDecision.Value, trend3Seconds, trend15Seconds, trend2Minutes, trend10Minutes, trend2Hours, this._cancellationTokenSource.Token).ConfigureAwait(false);
                }
                else if (lastDecision.Value.Action == "Sell"
                    && price * 0.99M < lastDecision.Value.Price
                    && trend10Minutes.IsValid() && trend10Minutes.Hours2 < 0
                    && trend10Minutes.IsValid() && trend10Minutes.Hours1 < 0
                    && trend2Minutes.IsValid() && trend2Minutes.Minutes15 > 0
                    && trend2Minutes.IsValid() && trend2Minutes.Minutes10 > 0
                    && trend15Seconds.IsValid() && trend15Seconds.Minutes3 < 0)
                {
                    this._logger.Information($"B {symbol} @ {Math.Round(price, 6).ToString("0000.000000")}: {_GetTrend(trend10Minutes, trend2Minutes, trend3Seconds)}");

                    lastDecision = new KeyValuePair<string, Decision>(symbol, new Decision()
                    {
                        Action = "Buy",
                        Price = price,
                        Symbol = symbol
                    });

                    if (this._rates.ContainsKey(symbol))
                    {
                        this._rates.Remove(symbol);
                    }

                    this._rates.Add(lastDecision.Key, lastDecision.Value);
                    await database.Insert(lastDecision.Value, trend3Seconds, trend15Seconds, trend2Minutes, trend10Minutes, trend2Hours, this._cancellationTokenSource.Token).ConfigureAwait(false);
                }
                else
                {
                    this._logger.Verbose($"N {symbol} @ {Math.Round(price, 6).ToString("0000.000000")}: {_GetTrend(trend10Minutes, trend2Minutes, trend3Seconds)}");
                }
            }
        }

        private string _GetTrend(Trend10Minutes trend10Minutes, Trend2Minutes trend2Minutes, Trend3Seconds trend3Seconds)
        {
            return $"@ 2hrs: {Math.Round(trend10Minutes.Hours2, 4)} | 10min: {Math.Round(trend2Minutes.Minutes10, 4)} | 30sec: {Math.Round(trend3Seconds.Seconds30, 4)} | 5sec: {Math.Round(trend3Seconds.Seconds5, 4)}";
        }

        public void Start()
        {
            this._makeDecision.Start();

            this._logger.Information("Decision maker started");
        }

        public void Stop()
        {
            this._makeDecision.Stop();

            this._cancellationTokenSource.Cancel();

            this._logger.Information("Decision maker stopped");
        }

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
                this._cancellationTokenSource.Dispose();

                this._buffer.Dispose();
            }

            this._disposed = true;
        }
    }
}
