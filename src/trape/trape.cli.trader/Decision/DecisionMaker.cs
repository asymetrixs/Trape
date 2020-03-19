using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using trape.cli.trader.Account;
using trape.cli.trader.Cache;
using trape.cli.trader.Cache.Models;
using trape.cli.trader.DataLayer;
using trape.cli.trader.trade;

namespace trape.cli.trader.Decision
{
    public class DecisionMaker : IDisposable, IDecisionMaker
    {
        private ILogger _logger;

        private IBuffer _buffer;

        private System.Threading.CancellationTokenSource _cancellationTokenSource;

        private Timer _makeDecision;

        private Dictionary<string, Decision> _lastDecision;

        private bool _disposed;


        public DecisionMaker(ILogger logger, IBuffer buffer)
        {
            if (null == logger || null == buffer)
            {
                throw new ArgumentNullException("Parameter cannot be NULL");
            }

            this._logger = logger;
            this._buffer = buffer;
            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this._lastDecision = new Dictionary<string, Decision>();
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
            foreach (var symbol in this._buffer.GetSymbols())
            {
                await _decide(symbol).ConfigureAwait(false);
            }
        }

        private async System.Threading.Tasks.Task _decide(string symbol)
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

            if (null == trend3Seconds || !trend3Seconds.IsValid()
                || null == trend15Seconds || !trend15Seconds.IsValid()
                || null == trend2Minutes || !trend2Minutes.IsValid()
                || null == trend10Minutes || !trend10Minutes.IsValid()
                || null == trend2Hours || !trend2Hours.IsValid())
            {
                this._logger.Warning($"Skipped {symbol} due to old or incomplete data");
            }

            var database = Pool.DatabasePool.Get();
            var accountant = Program.Services.GetService(typeof(IAccountant)) as IAccountant;

            this._lastDecision.TryGetValue(symbol, out Decision lastDecision);

            var assetBalance = accountant.GetBinanceBalance(symbol);
            var availableUSDT = accountant.GetBinanceBalance("USDT");
            var currentPrice = await database.GetCurrentPrice(symbol, this._cancellationTokenSource.Token).ConfigureAwait(false);

            if (this._lastDecision.ContainsKey(symbol))
            {
                this._lastDecision.Remove(symbol);
            }


            var veryShortImpact = trend3Seconds.Seconds5 + trend3Seconds.Seconds10 + trend3Seconds.Seconds15 + trend3Seconds.Seconds30;
            var shortImpact = trend15Seconds.Seconds45 + trend15Seconds.Minutes1 + trend15Seconds.Minutes2 + trend15Seconds.Minutes3;
            var midImpact = trend2Minutes.Minutes5 + trend2Minutes.Minutes7 + trend2Minutes.Minutes10 + trend2Minutes.Minutes15;
            var longImpact = trend10Minutes.Minutes30 + trend10Minutes.Hours1 + trend10Minutes.Hours2 + trend10Minutes.Hours3;
            var veryLongImpact = trend2Hours.Hours6 + trend2Hours.Hours12 + trend2Hours.Hours18 + trend2Hours.Day1;

            // kaufen 1 => 15min
            // 7 unter 25, 7+, 25+
            // (7 geht hoch ODER 7 geht runter UND 3 geht hoch)
            // 
            // verkaufen
            // 7 unter 25, 25+ > preis+
            // 7- =preis

            //var currentDecision = new Decision()
            //{
            //    Action = action,
            //    Price = currentPrice,
            //    Symbol = symbol,
            //    Indicator = _calculateIndicator(trend3Seconds, trend15Seconds, trend2Minutes, trend10Minutes, trend2Hours)
            //};

            //this._lastDecision.Add(symbol, currentDecision);

            //await database.Insert(currentDecision, trend3Seconds, trend15Seconds, trend2Minutes, trend10Minutes, trend2Hours, this._cancellationTokenSource.Token).ConfigureAwait(false);

            Pool.DatabasePool.Put(database);
            database = null;
        }

        private decimal _calculateIndicator(Trend3Seconds trend3Seconds, Trend15Seconds trend15Seconds, Trend2Minutes trend2Minutes, Trend10Minutes trend10Minutes, Trend2Hours trend2Hours)
        {


            return 0;
        }

        private string _GetTrend(Trend10Minutes trend10Minutes, Trend2Minutes trend2Minutes, Trend3Seconds trend3Seconds)
        {
            return $"@ 2/1: {Math.Round(trend10Minutes.Hours2, 4)}/{Math.Round(trend10Minutes.Hours1, 4)} | 10: {Math.Round(trend2Minutes.Minutes10, 4)} | 30: {Math.Round(trend3Seconds.Seconds30, 4)} | 5: {Math.Round(trend3Seconds.Seconds5, 4)}";
        }

        public Decision GetDecision(string symbol)
        {
            if (this._lastDecision.TryGetValue(symbol, out Decision decision))
            {
                return decision;
            }

            return null;
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

        public void ConfirmBuy(string symbol)
        {
            throw new NotImplementedException();
        }

        public int Recommendation(string symbol)
        {
            throw new NotImplementedException();
        }
    }
}
