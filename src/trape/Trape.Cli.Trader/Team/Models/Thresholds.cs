using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Trape.Cli.Trader.Team.Models
{
    public class Thresholds
    {
        /// <summary>
        /// Thresholds
        /// </summary>
        private readonly IOrderedEnumerable<Barrier> _thresholds;

        /// <summary>
        /// Threshold count
        /// </summary>
        private readonly int _thresholdsCount;

        /// <summary>
        /// Hit synchronization
        /// </summary>
        private SemaphoreSlim _hitSync;

        /// <summary>
        /// Initializes a new instance of the <see cref="Thresholds"/> class.
        /// </summary>
        /// <param name="initalPrice"></param>
        public Thresholds(decimal initalPrice)
        {
            _hitSync = new SemaphoreSlim(1, 1);

            var generatedList = new List<Barrier>
            {
                // Add 5% gain
                new Barrier() { Threshold = initalPrice * 1.05M }
            };

            // Start with 10%, end at 1500% gains
            for (decimal i = 1.1M; i < 15; i += 0.1M)
            {
                generatedList.Add(new Barrier() { Threshold = initalPrice * i });
            }

            _thresholds = generatedList.OrderBy(b => b.Threshold);
            _thresholdsCount = _thresholds.Count();
        }

        /// <summary>
        /// Marks the highest threshold that has been hit
        /// </summary>
        /// <param name="currentPrice"></param>
        public async Task Hit(decimal currentPrice)
        {
            await _hitSync.WaitAsync().ConfigureAwait(true);

            var currentPrice95pc = currentPrice * 0.95M;

            for (int i = 0; i < _thresholdsCount; i++)
            {
                var current = _thresholds.ElementAt(i);

                if (current.Threshold < currentPrice95pc)
                {
                    current.Hit = true;
                }
            }

            _hitSync.Release();
        }

        /// <summary>
        /// Indicates if the current price is dropping below a barrier that was hit earlier
        /// </summary>
        /// <param name="currentPrice"></param>
        /// <returns></returns>
        public bool IsDroppingBelowThreshold(decimal currentPrice)
        {
            return _thresholds.Any(t => t.Threshold > currentPrice && t.Hit);
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
