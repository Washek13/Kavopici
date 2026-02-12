namespace Kavopici.Services;

public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdateAsync();
    Task DownloadAndInstallUpdateAsync(UpdateInfo update, IProgress<double>? progress = null);
}

public record UpdateInfo(string Version, string DownloadUrl, string ReleaseNotes);
