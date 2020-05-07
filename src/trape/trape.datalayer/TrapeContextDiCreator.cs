using Microsoft.EntityFrameworkCore;
using Serilog;

namespace trape.datalayer
{
    public class TrapeContextDiCreator : ITrapeContextCreator
    {
        private readonly DbContextOptionsBuilder<TrapeContext> _dbContextOptionsBuilder;

        private readonly ILogger _logger;

        public TrapeContextDiCreator(DbContextOptionsBuilder<TrapeContext> configuration, ILogger logger)
        {
            this._dbContextOptionsBuilder = configuration;
            this._logger = logger;
        }

        public TrapeContext CreateDbContext()
        {
            return new TrapeContext(this._dbContextOptionsBuilder.Options, this._logger);
        }

        public DbContextOptionsBuilder<TrapeContext> Options(DbContextOptionsBuilder optionsBuilder)
        {
            return optionsBuilder as DbContextOptionsBuilder<TrapeContext>;
        }
    }
}
