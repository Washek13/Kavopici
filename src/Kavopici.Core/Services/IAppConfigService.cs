using Kavopici.Models;
using Kavopici.Models.Enums;

namespace Kavopici.Services;

public interface IAppConfigService
{
    /// <summary>
    /// Returns the persisted AppConfig row, or null if the database has never been initialized
    /// with a theme yet (brand-new database before the theme picker has run).
    /// </summary>
    Task<AppConfig?> GetOrNullAsync();

    /// <summary>
    /// Inserts the AppConfig row with the chosen theme. For a Tea database, also seeds the
    /// tea-specific tasting notes (Ids 9-16). Throws if a config row already exists.
    /// </summary>
    Task<AppConfig> CreateAsync(Theme theme);

    /// <summary>
    /// Returns the active theme. If no AppConfig row exists yet, lazily inserts one with
    /// Theme.Coffee (legacy databases predate this feature) and returns Coffee.
    /// </summary>
    Task<Theme> GetThemeAsync();
}
