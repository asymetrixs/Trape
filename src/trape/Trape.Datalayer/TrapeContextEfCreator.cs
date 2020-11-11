using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Trape.Datalayer
{
    public class TrapeContextEfCreator : ITrapeContextCreator, IDesignTimeDbContextFactory<TrapeContext>
    {
        private readonly IConfigurationRoot _configuartion;

        public TrapeContextEfCreator()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("settings.json");

            _configuartion = builder.Build();
        }

        public TrapeContext CreateDbContext()
        {
            var optionsBuilder = Options(new DbContextOptionsBuilder<TrapeContext>());

            return new TrapeContext(optionsBuilder.Options);
        }

        public TrapeContext CreateDbContext(string[] args)
        {
            var optionsBuilder = Options(new DbContextOptionsBuilder<TrapeContext>());

            return new TrapeContext(optionsBuilder.Options);
        }

        public DbContextOptionsBuilder<TrapeContext> Options(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuartion.GetConnectionString("trape-db"));

            return optionsBuilder as DbContextOptionsBuilder<TrapeContext>;
        }
    }

}
