namespace Kavopici.Services;

public interface IAppSettingsService
{
    string? DatabasePath { get; }
    IReadOnlyList<string> RecentDatabasePaths { get; }
    void SetDatabasePath(string? path);
    void RemoveRecentDatabasePath(string path);
    void Load();
}
