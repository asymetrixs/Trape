using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Trape.Cli.trader.Listener
{
    /// <summary>
    /// This class hols information about a best price
    /// </summary>
    public class BestPrice : IDisposable
    {
        #region Fields

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Buffer size for the price buffer
        /// </summary>
        private const long _bufferSize = 10;

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

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>BestPrice</c> class
        /// </summary>
        /// <param name="symbol"></param>
        public BestPrice()
        {
            _disposed = false;
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
            // Check if last value is more recent than 3 seconds ago
            if (_lastUpdate < (DateTime.UtcNow.Ticks - _3secondsInTicks))
            {
                return -1;
            }

            // Calculate average over array
            return _prices.Average();
        }

        /// <summary>
        /// Returns the latest price
        /// </summary>
        /// <returns></returns>
        public decimal? Latest => _prices[_position];

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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _syncAdd.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
