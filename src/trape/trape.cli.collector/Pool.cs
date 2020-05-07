using trape.datalayer;

namespace trape.cli.collector
{
    /// <summary>
    /// This class holds pools that cache instances
    /// </summary>
    public static class Pool
    {
        #region Fields

        /// <summary>
        /// Holds Database objects
        /// </summary>
        //public static ObjectPool<TrapeContext> DatabasePool { get; private set; }

        #endregion

        #region Functions

        /// <summary>
        /// Initializes the pools
        /// </summary>
        public static void Initialize()
        {
            //DatabasePool = new ObjectPool<TrapeContext>(() => Program.Services.GetService(typeof(TrapeContext)) as TrapeContext);

            // Warmup
            //DatabasePool.Warmup(32);
        }

        #endregion
    }
}
