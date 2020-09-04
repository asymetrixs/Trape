namespace trape.datalayer
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.Extensions.Configuration;
    using Serilog;

    public class TrapeContextEfCreator : ITrapeContextCreator, IDesignTimeDbContextFactory<TrapeContext>
    {
        private readonly IConfigurationRoot _configuartion;

        public TrapeContextEfCreator()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("settings.json");
            
            this._configuartion = builder.Build();
        }

        public TrapeContext CreateDbContext()
        {
            var optionsBuilder = this.Options(new DbContextOptionsBuilder<TrapeContext>());

            return new TrapeContext(optionsBuilder.Options);
        }

        public TrapeContext CreateDbContext(string[] args)
        {
            var optionsBuilder = this.Options(new DbContextOptionsBuilder<TrapeContext>());

            return new TrapeContext(optionsBuilder.Options);
        }

        public DbContextOptionsBuilder<TrapeContext> Options(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(this._configuartion.GetConnectionString("trape-db"));

            return optionsBuilder as DbContextOptionsBuilder<TrapeContext>;
        }
    }

}
