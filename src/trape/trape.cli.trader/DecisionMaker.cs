using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using trape.cli.trader.DataLayer;

namespace trape.cli.trader
{
    public class DecisionMaker
    {
        private ILogger _logger;

        private Cache.Buffer _buffer;

        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        private Timer _makeDecision;

        private Dictionary<string, Decision> _rates;

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

            this._makeDecision = new Timer()
            {
                AutoReset = true,
                Interval = new TimeSpan(0, 0, 30).TotalMilliseconds
            };
            this._makeDecision.Elapsed += _makeDecision_Elapsed;
        }

        private async void _makeDecision_Elapsed(object sender, ElapsedEventArgs e)
        {
            var database = Program.Services.GetService(typeof(ITrapeContext)) as ITrapeContext;

            foreach (var symbol in this._buffer.GetSymbols())
            {
                var trend3Seconds = this._buffer.Trends3Seconds.Single(t => t.Symbol == symbol);
                var trend15Seconds = this._buffer.Trends15Seconds.Single(t => t.Symbol == symbol);
                var trend2Minutes = this._buffer.Trends2Minutes.Single(t => t.Symbol == symbol);
                var trend10Minutes = this._buffer.Trends10Minutes.Single(t => t.Symbol == symbol);
                var trend2Hours = this._buffer.Trends2Hours.Single(t => t.Symbol == symbol);

                var lastDecision = this._rates.FirstOrDefault(d => d.Key == symbol);

                var price = await database.GetCurrentPrice(symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);

                // No trade yet
                if (!this._rates.ContainsKey(symbol))
                {
                    if (trend10Minutes.Hours2 < 0 && trend2Minutes.Minutes10 > 0)
                    {
                        lastDecision = new KeyValuePair<string, Decision>(symbol, new Decision()
                        {
                            Action = "Buy",
                            Price = price,
                            Symbol = symbol
                        });

                        this._logger.Verbose($"Added {symbol}");
                    }
                    else
                    {
                        this._logger.Verbose($"Skipping {symbol}: Trends are 2hrs: {trend10Minutes.Hours2}   10min: {trend2Minutes.Minutes10}");
                    }
                }
                else if (lastDecision.Value.Action == "Buy"
                    && trend10Minutes.Hours2 > 0 && trend10Minutes.Hours1 > 0 && trend2Minutes.Minutes15 < 0 && trend2Minutes.Minutes10 < 0 && trend15Seconds.Minutes3 > 0
                    && price * 1.01M > lastDecision.Value.Price)
                {
                    this._logger.Information($"Selling {symbol} for {price}");

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

                    this._rates.Add(lastDecision.Key, lastDecision.Value);
                }
                else if (lastDecision.Value.Action == "Sell"
                    && trend10Minutes.Hours2 < 0 && trend10Minutes.Hours1 < 0 && trend2Minutes.Minutes15 > 0 && trend2Minutes.Minutes10 > 0 && trend15Seconds.Minutes3 < 0
                    && price * 0.99M < lastDecision.Value.Price)
                {
                    this._logger.Information($"Buying {symbol} for {price}");

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
                }
                else
                {
                    this._logger.Verbose($"No action for {symbol}");
                }
            }
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

        private class Decision
        {
            public string Symbol;

            public decimal Price;

            public string Action;
        }
    }
}
