using trape.datalayer;

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
        //public static ObjectPool<TrapeContext> DatabasePool { get; private set; }

        #endregion

        #region Functions

        /// <summary>
        /// Initializes the Pool
        /// </summary>
        public static void Initialize()
        {
            //DatabasePool = new ObjectPool<TrapeContext>(() => Program.Services.GetService(typeof(TrapeContext)) as TrapeContext);

            // Warmup
            //DatabasePool.Warmup(10);
        }

        #endregion
    }
}
