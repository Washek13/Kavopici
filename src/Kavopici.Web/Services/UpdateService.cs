using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using Kavopici.Services;

namespace Kavopici.Web.Services;

public class UpdateService : IUpdateService
{
    private const string GitHubOwner = "Washek13";
    private const string GitHubRepo = "Kavopici";
    private const string GitHubApiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
    private const int DownloadBufferSize = 81920; // 80 KB

    private readonly IHostApplicationLifetime _lifetime;

    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "Kavopici-Updater" },
            { "Accept", "application/vnd.github.v3+json" }
        }
    };

    public UpdateService(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        // MSIX updates are only supported on Windows
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            var release = await Http.GetFromJsonAsync<GitHubRelease>(GitHubApiUrl);
            if (release is null) return null;

            var currentVersion = GetCurrentVersion();
            var latestVersion = Version.Parse(release.TagName.TrimStart('v'));

            if (latestVersion <= currentVersion)
                return null;

            var msixAsset = release.Assets?.FirstOrDefault(a =>
                a.Name.EndsWith(".msix", StringComparison.OrdinalIgnoreCase));

            if (msixAsset is null) return null;

            return new UpdateInfo(
                release.TagName,
                msixAsset.BrowserDownloadUrl,
                release.Body ?? "");
        }
        catch
        {
            // Update check is non-critical — fail silently
            return null;
        }
    }

    public async Task DownloadAndInstallUpdateAsync(
        UpdateInfo update, IProgress<double>? progress = null)
    {
        // MSIX installation is only supported on Windows
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Auto-update is only supported on Windows");

        var tempPath = Path.Combine(Path.GetTempPath(), $"Kavopici-{update.Version}.msix");

        using var response = await Http.GetAsync(
            update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(
            tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[DownloadBufferSize];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;
            if (totalBytes > 0)
                progress?.Report((double)totalRead / totalBytes);
        }

        fileStream.Close();

        // Launch the MSIX installer and stop the web host
        Process.Start(new ProcessStartInfo
        {
            FileName = tempPath,
            UseShellExecute = true
        });

        _lifetime.StopApplication();
    }

    private static Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version
               ?? new Version(0, 0, 0);
    }

    private record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("assets")] GitHubAsset[]? Assets);

    private record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
}
