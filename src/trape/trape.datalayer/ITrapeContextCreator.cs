using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace trape.datalayer
{
    public interface ITrapeContextCreator
    {
        TrapeContext CreateDbContext();

        DbContextOptionsBuilder<TrapeContext> Options(DbContextOptionsBuilder optionsBuilder);
    }
}
