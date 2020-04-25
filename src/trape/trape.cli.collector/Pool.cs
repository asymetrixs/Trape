using Newtonsoft.Json.Serialization;
using Serilog;
using trape.cli.collector.DataLayer;

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
        public static ObjectPool<ITrapeContext> DatabasePool { get; private set; }

        #endregion

        #region Functions

        /// <summary>
        /// Initializes the pools
        /// </summary>
        public static void Initialize()
        {
            DatabasePool = new ObjectPool<ITrapeContext>(() => Program.Services.GetService(typeof(ITrapeContext)) as ITrapeContext);
        }

        #endregion
    }
}
