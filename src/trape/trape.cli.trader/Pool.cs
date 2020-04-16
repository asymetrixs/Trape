﻿using Serilog;
using trape.cli.trader.DataLayer;

namespace trape.cli.trader
{
    /// <summary>
    /// Holds instances of reusable objects
    /// </summary>
    public static class Pool
    {
        #region Fields

        /// <summary>
        /// Holds instances of TrapeContext
        /// </summary>
        public static ObjectPool<ITrapeContext> DatabasePool { get; private set; }

        #endregion

        #region Functions

        /// <summary>
        /// Initializes the Pool
        /// </summary>
        public static void Initialize()
        {
            DatabasePool = new ObjectPool<ITrapeContext>(() => new TrapeContext(Program.Services.GetService(typeof(ILogger)) as ILogger));

            // Warmup
            DatabasePool.Warmup(10);
        }

        #endregion
    }
}
