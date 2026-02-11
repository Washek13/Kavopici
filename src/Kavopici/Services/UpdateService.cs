using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Kavopici.Services;

public class UpdateService : IUpdateService
{
    private const string GitHubOwner = "Washek13";
    private const string GitHubRepo = "Kavopici";

    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "Kavopici-Updater" },
            { "Accept", "application/vnd.github.v3+json" }
        }
    };

    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
            var release = await Http.GetFromJsonAsync<GitHubRelease>(url);
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
        var tempPath = Path.Combine(Path.GetTempPath(), $"Kavopici-{update.Version}.msix");

        using var response = await Http.GetAsync(
            update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(
            tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
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

        // Launch the MSIX installer and exit the app
        Process.Start(new ProcessStartInfo
        {
            FileName = tempPath,
            UseShellExecute = true
        });

        System.Windows.Application.Current.Shutdown();
    }

    private static Version GetCurrentVersion()
    {
        try
        {
            var packageVersion = Windows.ApplicationModel.Package.Current.Id.Version;
            return new Version(
                packageVersion.Major, packageVersion.Minor,
                packageVersion.Build, packageVersion.Revision);
        }
        catch
        {
            // Fallback for non-MSIX (development) builds
            return Assembly.GetExecutingAssembly().GetName().Version
                   ?? new Version(0, 0, 0);
        }
    }

    private record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("assets")] GitHubAsset[]? Assets);

    private record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
}
