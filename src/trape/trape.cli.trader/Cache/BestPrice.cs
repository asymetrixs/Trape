using System;
using System.Linq;
using System.Threading;

namespace trape.cli.trader.Cache
{
    public class BestPrice
    {
        #region Fields

        private const long _bufferSize = 30;

        private long _position;

        private decimal[] _prices;

        private long _lastUpdate;

        private static readonly long _3secondsInTicks = new TimeSpan(0, 0, 3).Ticks;

        public string Symbol { get; private set; }
        
        #endregion

        #region Constructor

        public BestPrice(string symbol)
        {
            this.Symbol = symbol;
            this._position = 0;
            this._prices = new decimal[_bufferSize];

            // Initialize with an obsolete value
            this._lastUpdate = DateTime.UtcNow.AddMinutes(-1).Ticks;
        }

        #endregion

        #region

        public void Add(decimal price)
        {
            Monitor.Enter(this._position);

            // May overflow at some point
            unchecked
            {
                _prices[_position % _bufferSize] = price;
                // Move cursor ahead
                ++_position;
                this._lastUpdate = DateTime.UtcNow.Ticks;
            }

            Monitor.Exit(this._position);
        }

        /// <summary>
        /// Calculates the average price over the last 30 prices
        /// </summary>
        /// <returns>Returns -1 if there is no recent (3 seconds) data, otherwise the average over the last 30 prices.</returns>
        public decimal GetAverage()
        {
            decimal average;

            // Check if last value is more recent than 3 seconds ago
            if(this._lastUpdate < (DateTime.UtcNow.Ticks - _3secondsInTicks))
            {
                return -1;
            }

            Monitor.Enter(this._position);

            average = this._prices.Average();

            Monitor.Exit(this._position);

            return average;
        }

        #endregion
    }
}
