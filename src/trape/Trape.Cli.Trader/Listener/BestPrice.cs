namespace Trape.Cli.Trader.Listener
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class hols information about a best price
    /// </summary>
    public class BestPrice : IDisposable
    {
        /// <summary>
        /// Buffer size for the price buffer
        /// </summary>
        private const long _bufferSize = 10;

        /// <summary>
        /// 3 seconds in ticks
        /// </summary>
        private static readonly long _3secondsInTicks = new TimeSpan(0, 0, 3).Ticks;

        /// <summary>
        /// Synchronize adding of values
        /// </summary>
        private readonly SemaphoreSlim _syncAdd;

        /// <summary>
        /// Array holding the prices
        /// </summary>
        private readonly decimal[] _prices;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Current position in the buffer
        /// </summary>
        private long _position;

        /// <summary>
        /// Date of last update
        /// </summary>
        private long _lastUpdate;

        /// <summary>
        /// Initializes a new instance of the <c>BestPrice</c> class
        /// </summary>
        /// <param name="symbol">Symbol</param>
        public BestPrice()
        {
            this._disposed = false;
            this._position = -1;
            this._prices = new decimal[_bufferSize];
            this._syncAdd = new SemaphoreSlim(1, 1);

            // Initialize with an obsolete value
            this._lastUpdate = DateTime.UtcNow.AddMinutes(-1).Ticks;
        }

        /// <summary>
        /// Returns the latest price
        /// </summary>
        public decimal? Latest => this._prices[this._position];

        /// <summary>
        /// Adds a new price to the array
        /// </summary>
        /// <param name="price">Price</param>
        public async Task Add(decimal price)
        {
            // Enter synchronized context
            await this._syncAdd.WaitAsync().ConfigureAwait(true);

            try
            {
                // Variable _position overflows at some point
                unchecked
                {
                    // Move cursor ahead
                    this._prices[++this._position % _bufferSize] = price;
                }

                // Update time of addition
                this._lastUpdate = DateTime.UtcNow.Ticks;
            }
            finally
            {
                this._syncAdd.Release();
            }
        }

        /// <summary>
        /// Calculates the average price over the last 30 prices
        /// </summary>
        /// <returns>Returns -1 if there is no recent (3 seconds) data, otherwise the average over the last 30 prices.</returns>
        public decimal GetAverage()
        {
            // Check if last value is more recent than 3 seconds ago
            if (this._lastUpdate < (DateTime.UtcNow.Ticks - _3secondsInTicks))
            {
                return -1;
            }

            // Calculate average over array
            return this._prices.Average();
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._syncAdd.Dispose();
            }

            this._disposed = true;
        }
    }
}
