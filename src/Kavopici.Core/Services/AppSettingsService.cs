using System.IO;
using System.Text.Json;

namespace Kavopici.Services;

public class AppSettingsService : IAppSettingsService
{
    private const int MaxRecentPaths = 5;

    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kavopici");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    private AppSettings _settings = new();

    public string? DatabasePath => _settings.DatabasePath;

    public IReadOnlyList<string> RecentDatabasePaths => _settings.RecentDatabasePaths.AsReadOnly();

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
        if (string.IsNullOrEmpty(path))
        {
            _settings.DatabasePath = null;
        }
        else
        {
            var expanded = ExpandPath(path);
            _settings.DatabasePath = expanded;

            _settings.RecentDatabasePaths.RemoveAll(p => string.Equals(p, expanded, StringComparison.OrdinalIgnoreCase));
            _settings.RecentDatabasePaths.Insert(0, expanded);
            if (_settings.RecentDatabasePaths.Count > MaxRecentPaths)
                _settings.RecentDatabasePaths.RemoveRange(MaxRecentPaths, _settings.RecentDatabasePaths.Count - MaxRecentPaths);
        }

        Save();
    }

    public void RemoveRecentDatabasePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        var removed = _settings.RecentDatabasePaths.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
            Save();
    }

    public static string ExpandPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var trimmed = path.Trim();

        // Handle tilde expansion (Unix/macOS/Linux)
        if (trimmed.StartsWith("~/") || trimmed == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            trimmed = trimmed.Length == 1 ? home : Path.Combine(home, trimmed.Substring(2));
        }

        // Expand environment variables (%VAR% on Windows, $VAR on Unix)
        trimmed = Environment.ExpandEnvironmentVariables(trimmed);

        // Convert to absolute path if relative
        try
        {
            return Path.GetFullPath(trimmed);
        }
        catch
        {
            // If path is invalid, return original trimmed path and let caller handle
            return trimmed;
        }
    }

    public static string GetPlatformPlaceholderPath()
        => OperatingSystem.IsWindows()
            ? @"C:\Users\jmeno\Documents\kavopici.db"
            : "~/Documents/kavopici.db";

    private void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFile, json);
    }

    private class AppSettings
    {
        public string? DatabasePath { get; set; }
        public List<string> RecentDatabasePaths { get; set; } = new();
    }
}
