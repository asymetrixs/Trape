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
            _dbContextOptionsBuilder = configuration;
            _logger = logger;
        }

        public TrapeContext CreateDbContext()
        {
            return new TrapeContext(_dbContextOptionsBuilder.Options);
        }

        public DbContextOptionsBuilder<TrapeContext> Options(DbContextOptionsBuilder optionsBuilder)
        {
            return optionsBuilder as DbContextOptionsBuilder<TrapeContext>;
        }
    }
}
