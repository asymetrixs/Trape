using System;

namespace trape.cli.trader.WatchDog
{
    public interface IActive
    {
        /// <summary>
        /// Last time item was active
        /// </summary>
        public DateTime LastActive { get; }
    }
}
