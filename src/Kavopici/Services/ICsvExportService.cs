namespace Kavopici.Services;

public interface ICsvExportService
{
    Task ExportStatisticsAsync(string filePath);
}
