using Kavopici.Services;
using Microsoft.EntityFrameworkCore;

namespace Kavopici.Data;

public class KavopiciDbContextFactory : IDbContextFactory<KavopiciDbContext>
{
    private readonly IAppSettingsService _settings;

    public KavopiciDbContextFactory(IAppSettingsService settings)
    {
        _settings = settings;
    }

    public KavopiciDbContext CreateDbContext()
    {
        var path = _settings.DatabasePath
            ?? throw new InvalidOperationException("Není vybrána žádná databáze.");

        var options = new DbContextOptionsBuilder<KavopiciDbContext>()
            .UseSqlite($"Data Source={path}")
            .AddInterceptors(new SqliteWalInterceptor())
            .Options;

        return new KavopiciDbContext(options);
    }
}
