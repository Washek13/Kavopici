using Kavopici.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kavopici.Tests.Helpers;

public class TestDbContextFactory : IDbContextFactory<KavopiciDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<KavopiciDbContext> _options;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<KavopiciDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the schema
        using var context = new KavopiciDbContext(_options);
        context.Database.EnsureCreated();
    }

    public KavopiciDbContext CreateDbContext()
    {
        return new KavopiciDbContext(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
