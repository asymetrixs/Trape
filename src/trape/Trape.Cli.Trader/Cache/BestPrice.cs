using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Trape.Cli.trader.Cache
{
    /// <summary>
    /// This class hols information about a best price
    /// </summary>
    public class BestPrice
    {
        #region Fields

        /// <summary>
        /// Buffer size for the price buffer
        /// </summary>
        private const long _bufferSize = 15;

        /// <summary>
        /// Current position in the buffer
        /// </summary>
        private long _position;

        /// <summary>
        /// Synchronize adding of values
        /// </summary>
        private readonly SemaphoreSlim _syncAdd;

        /// <summary>
        /// Array holding the prices
        /// </summary>
        private readonly decimal[] _prices;

        /// <summary>
        /// Date of last update
        /// </summary>
        private long _lastUpdate;

        /// <summary>
        /// 3 seconds in ticks
        /// </summary>
        private static readonly long _3secondsInTicks = new TimeSpan(0, 0, 3).Ticks;

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>BestPrice</c> class
        /// </summary>
        /// <param name="symbol"></param>
        public BestPrice(string symbol)
        {
            Symbol = symbol;
            _position = -1;
            _prices = new decimal[_bufferSize];
            _syncAdd = new SemaphoreSlim(1, 1);

            // Initialize with an obsolete value
            _lastUpdate = DateTime.UtcNow.AddMinutes(-1).Ticks;
        }

        #endregion

        #region

        /// <summary>
        /// Adds a new price to the array
        /// </summary>
        /// <param name="price"></param>
        public async Task Add(decimal price)
        {
            // Enter synchronized context
            await _syncAdd.WaitAsync().ConfigureAwait(true);

            try
            {
                // Variable _position overflows at some point
                unchecked
                {
                    // Move cursor ahead
                    _prices[++_position % _bufferSize] = price;
                }

                // Update time of addition
                _lastUpdate = DateTime.UtcNow.Ticks;
            }
            finally
            {
                _syncAdd.Release();
            }
        }

        /// <summary>
        /// Calculates the average price over the last 30 prices
        /// </summary>
        /// <returns>Returns -1 if there is no recent (3 seconds) data, otherwise the average over the last 30 prices.</returns>
        public decimal GetAverage()
        {
            decimal average;

            // Check if last value is more recent than 3 seconds ago
            if (_lastUpdate < (DateTime.UtcNow.Ticks - _3secondsInTicks))
            {
                return -1;
            }

            // Calculate average over array
            average = _prices.Average();

            return average;
        }

        #endregion
    }
}
