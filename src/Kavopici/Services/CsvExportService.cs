using System.IO;
using System.Text;

namespace Kavopici.Services;

public class CsvExportService : ICsvExportService
{
    private readonly IStatisticsService _statisticsService;

    public CsvExportService(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public async Task ExportStatisticsAsync(string filePath)
    {
        var stats = await _statisticsService.GetBlendStatisticsAsync();
        var sb = new StringBuilder();

        // UTF-8 BOM will be written by StreamWriter
        sb.AppendLine("Název směsi;Pražírna;Původ;Stupeň pražení;Dodavatel;Průměrné hodnocení;Počet hodnocení;1★;2★;3★;4★;5★");

        foreach (var s in stats)
        {
            var roastLevel = s.RoastLevel switch
            {
                Models.Enums.RoastLevel.Light => "Lehké",
                Models.Enums.RoastLevel.MediumLight => "Středně lehké",
                Models.Enums.RoastLevel.Medium => "Střední",
                Models.Enums.RoastLevel.MediumDark => "Středně tmavé",
                Models.Enums.RoastLevel.Dark => "Tmavé",
                _ => s.RoastLevel.ToString()
            };

            sb.AppendLine(string.Join(";",
                Escape(s.BlendName),
                Escape(s.Roaster),
                Escape(s.Origin ?? ""),
                Escape(roastLevel),
                Escape(s.SupplierName),
                s.AverageRating.ToString("F2"),
                s.RatingCount.ToString(),
                s.Distribution[0].ToString(),
                s.Distribution[1].ToString(),
                s.Distribution[2].ToString(),
                s.Distribution[3].ToString(),
                s.Distribution[4].ToString()
            ));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Escape(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
