using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kavopici.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KavopiciDbContext>
{
    public KavopiciDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KavopiciDbContext>();
        optionsBuilder.UseSqlite("Data Source=kavopici_design.db");
        return new KavopiciDbContext(optionsBuilder.Options);
    }
}
