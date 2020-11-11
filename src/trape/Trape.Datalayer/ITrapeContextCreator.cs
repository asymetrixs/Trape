using Microsoft.EntityFrameworkCore;

namespace Trape.Datalayer
{
    public interface ITrapeContextCreator
    {
        TrapeContext CreateDbContext();

        DbContextOptionsBuilder<TrapeContext> Options(DbContextOptionsBuilder optionsBuilder);
    }
}
