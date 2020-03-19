using Serilog;
using trape.cli.collector.DataLayer;

namespace trape.cli.collector
{
    public static class Pool
    {
        public static ObjectPool<ITrapeContext> DatabasePool { get; private set; }

        public static void Initialize()
        {
            DatabasePool = new ObjectPool<ITrapeContext>(() => new TrapeContext(Program.Services.GetService(typeof(ILogger)) as ILogger));
        }
    }
}
