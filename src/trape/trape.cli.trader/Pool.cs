using Serilog;
using trape.cli.trader.DataLayer;

namespace trape.cli.trader
{
    public static class Pool
    {
        #region Fields

        public static ObjectPool<ITrapeContext> DatabasePool { get; private set; }

        #endregion

        #region Functions

        public static void Initialize()
        {
            DatabasePool = new ObjectPool<ITrapeContext>(() => new TrapeContext(Program.Services.GetService(typeof(ILogger)) as ILogger));
        }

        #endregion
    }
}
