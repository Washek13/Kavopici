namespace Kavopici.Services;

public interface IAppSettingsService
{
    string? DatabasePath { get; }
    void SetDatabasePath(string? path);
    void Load();
}
