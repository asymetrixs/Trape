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

        public string Symbol { get; private set; }
        
        #endregion

        #region Constructor

        public BestPrice(string symbol)
        {
            this.Symbol = symbol;
            this._position = 0;
            this._prices = new decimal[_bufferSize];
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
            }

            Monitor.Exit(this._position);
        }

        public decimal GetAverage()
        {
            decimal average;

            Monitor.Enter(this._position);

            average = this._prices.Average();

            Monitor.Exit(this._position);

            return average;
        }

        #endregion
    }
}
