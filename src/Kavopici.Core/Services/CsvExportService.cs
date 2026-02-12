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
        var bytes = await GenerateCsvBytesAsync();
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    public async Task<byte[]> GenerateCsvBytesAsync()
    {
        var stats = await _statisticsService.GetBlendStatisticsAsync();
        var sb = new StringBuilder();

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

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(sb.ToString())).ToArray();
    }

    private static string Escape(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
