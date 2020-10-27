using Microsoft.EntityFrameworkCore;

namespace trape.datalayer
{
    public interface ITrapeContextCreator
    {
        TrapeContext CreateDbContext();

        DbContextOptionsBuilder<TrapeContext> Options(DbContextOptionsBuilder optionsBuilder);
    }
}
