using System.IO;
using System.Text.Json;

namespace Kavopici.Services;

public class AppSettingsService : IAppSettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kavopici");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    private AppSettings _settings = new();

    public string? DatabasePath => _settings.DatabasePath;

    public void Load()
    {
        if (!File.Exists(SettingsFile))
            return;

        try
        {
            var json = File.ReadAllText(SettingsFile);
            _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    public void SetDatabasePath(string? path)
    {
        _settings.DatabasePath = path;
        Save();
    }

    private void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFile, json);
    }

    private class AppSettings
    {
        public string? DatabasePath { get; set; }
    }
}
