using Kavopici.Data;
using Kavopici.Models;
using Kavopici.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kavopici.Services;

public class AppConfigService : IAppConfigService
{
    private readonly IDbContextFactory<KavopiciDbContext> _contextFactory;

    // Tea tasting notes seeded when a Tea database is created. Names mirror the broad
    // families on standard tea flavor wheels (e.g. https://pathofcha.com/pages/tea-flavor-wheel).
    private static readonly string[] TeaNoteNames =
    {
        "Květinová",
        "Ovocná",
        "Travnatá",
        "Ořechová",
        "Zemitá",
        "Dřevitá",
        "Kořeněná",
        "Sladká"
    };

    public AppConfigService(IDbContextFactory<KavopiciDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<AppConfig?> GetOrNullAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AppConfigs.FirstOrDefaultAsync();
    }

    public async Task<AppConfig> CreateAsync(Theme theme)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.AppConfigs.AnyAsync())
            throw new InvalidOperationException("AppConfig already exists.");

        var config = new AppConfig
        {
            Theme = theme,
            CreatedAt = DateTime.UtcNow
        };
        context.AppConfigs.Add(config);

        if (theme == Theme.Tea)
        {
            // Only insert tea notes if they don't already exist (defensive — e.g. retry path).
            var hasTeaNotes = await context.TastingNotes.AnyAsync(n => n.Theme == Theme.Tea);
            if (!hasTeaNotes)
            {
                foreach (var name in TeaNoteNames)
                {
                    context.TastingNotes.Add(new TastingNote
                    {
                        Name = name,
                        Theme = Theme.Tea
                    });
                }
            }
        }

        await context.SaveChangesAsync();
        return config;
    }

    public async Task<Theme> GetThemeAsync()
    {
        var config = await GetOrNullAsync();
        if (config != null)
            return config.Theme;

        // Legacy database — predates the theme feature. Default to Coffee and persist
        // so subsequent reads are consistent and the theme travels with the file.
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (await context.AppConfigs.AnyAsync())
            return (await context.AppConfigs.FirstAsync()).Theme;

        var legacy = new AppConfig { Theme = Theme.Coffee, CreatedAt = DateTime.UtcNow };
        context.AppConfigs.Add(legacy);
        await context.SaveChangesAsync();
        return Theme.Coffee;
    }
}
