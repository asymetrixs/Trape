namespace Trape.Cli.Trader.Team.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class build up an internal scale for the price of an asset.
    /// Steps are +5%, then +10% until it hits 1500%.
    /// The price is supposed to rise and cross some of the steps.
    /// Once the price declines, the class checks if it falls below a step it has already passed when rising.
    /// If this is the case, then the broker is supposed to sell the asset.
    /// </summary>
    public class Thresholds : IDisposable
    {
        /// <summary>
        /// Hit synchronization
        /// </summary>
        private readonly SemaphoreSlim _hitSync;

        /// <summary>
        /// Thresholds
        /// </summary>
        private IOrderedEnumerable<Barrier> _thresholds;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Threshold count
        /// </summary>
        private int _thresholdsCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Thresholds"/> class.
        /// </summary>
        public Thresholds()
        {
            this._hitSync = new SemaphoreSlim(1, 1);
            this._thresholds = Array.Empty<Barrier>().OrderBy(a => a);
            this._thresholdsCount = 0;
            this._disposed = false;
        }

        /// <summary>
        /// Returns true if this instance was initialized
        /// </summary>
        public bool IsInitialized => this._thresholdsCount != 0;

        /// <summary>
        /// Marks the highest threshold that has been hit
        /// </summary>
        /// <param name="currentPrice">Current Price</param>
        public async Task Hit(decimal currentPrice)
        {
            await this._hitSync.WaitAsync().ConfigureAwait(true);

            // Check if is not initialized
            if (!this.IsInitialized)
            {
                this.Init(currentPrice);
            }

            var currentPrice95pc = currentPrice * 0.95M;

            for (int i = 0; i < this._thresholdsCount; i++)
            {
                var current = this._thresholds.ElementAt(i);

                if (current.Threshold < currentPrice95pc)
                {
                    current.Hit = true;
                }
            }

            this._hitSync.Release();
        }

        /// <summary>
        /// Indicates if the current price is dropping below a barrier that was hit earlier
        /// </summary>
        /// <param name="currentPrice">Current Price</param>
        public bool IsDroppingBelowThreshold(decimal currentPrice)
        {
            return this._thresholds.Any(t => t.Threshold > currentPrice && t.Hit);
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
                this._hitSync.Dispose();
            }

            this._disposed = true;
        }

        /// <summary>
        /// Initializes the threshold
        /// </summary>
        /// <param name="initialPrice">Initial Price</param>
        private void Init(decimal initialPrice)
        {
            var generatedList = new List<Barrier>(152)
            {
                // Add 5% gain
                new Barrier() { Threshold = initialPrice * 1.05M },
            };

            // Start with 10%, end at 1500% gains
            for (decimal i = 1.1M; i <= 15; i += 0.1M)
            {
                generatedList.Add(new Barrier() { Threshold = initialPrice * i });
            }

            this._thresholds = generatedList.OrderBy(b => b.Threshold);
            this._thresholdsCount = this._thresholds.Count();
        }

        private class Barrier
        {
            /// <summary>
            /// Threshold
            /// </summary>
            public decimal Threshold { get; init; }

            /// <summary>
            /// Flag indicating of threshold was hit
            /// </summary>
            public bool Hit { get; set; }
        }
    }
}
