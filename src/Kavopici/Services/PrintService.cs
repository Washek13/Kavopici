using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Kavopici.Services;

public class PrintService : IPrintService
{
    private readonly IStatisticsService _statisticsService;

    private static readonly SolidColorBrush CoffeeDark = new(Color.FromRgb(0x2C, 0x18, 0x10));
    private static readonly SolidColorBrush CoffeeMedium = new(Color.FromRgb(0x4A, 0x2C, 0x2A));
    private static readonly SolidColorBrush AmberGold = new(Color.FromRgb(0xD4, 0xA0, 0x17));
    private static readonly SolidColorBrush Silver = new(Color.FromRgb(0xA5, 0xAA, 0xAF));

    public PrintService(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public async Task PrintStatisticsReportAsync()
    {
        var stats = await _statisticsService.GetBlendStatisticsAsync();

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Arial"),
            FontSize = 12,
            PagePadding = new Thickness(50),
            ColumnWidth = double.MaxValue
        };

        // Title
        doc.Blocks.Add(new Paragraph(new Run("KÁVOPÍČI — PŘEHLED HODNOCENÍ"))
        {
            FontFamily = new FontFamily("Arial Narrow"),
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = CoffeeDark,
            Margin = new Thickness(0, 0, 0, 4)
        });

        // Date
        doc.Blocks.Add(new Paragraph(new Run($"Datum: {DateTime.Now:d. MMMM yyyy}"))
        {
            FontSize = 10,
            Foreground = Silver,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Stats table
        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        // Header row
        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = CoffeeMedium };
        headerRow.Cells.Add(CreateCell("SMĚS", true, Brushes.White));
        headerRow.Cells.Add(CreateCell("PRAŽÍRNA", true, Brushes.White));
        headerRow.Cells.Add(CreateCell("PRŮMĚR", true, Brushes.White));
        headerRow.Cells.Add(CreateCell("POČET", true, Brushes.White));
        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        // Data rows
        var dataGroup = new TableRowGroup();
        var alt = false;
        foreach (var s in stats)
        {
            var row = new TableRow();
            if (alt) row.Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xF5, 0xEF));

            row.Cells.Add(CreateCell(s.BlendName, true));
            row.Cells.Add(CreateCell(s.Roaster));
            row.Cells.Add(CreateCell($"{s.AverageRating:F1} ★"));
            row.Cells.Add(CreateCell(s.RatingCount.ToString()));
            dataGroup.Rows.Add(row);
            alt = !alt;
        }
        table.RowGroups.Add(dataGroup);
        doc.Blocks.Add(table);

        // Top 3 highlights
        if (stats.Count >= 1)
        {
            doc.Blocks.Add(new Paragraph(new Run("TOP 3"))
            {
                FontFamily = new FontFamily("Arial Narrow"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = CoffeeDark,
                Margin = new Thickness(0, 20, 0, 8)
            });

            var top = stats.OrderByDescending(s => s.AverageRating).Take(3).ToList();
            var list = new List { MarkerStyle = TextMarkerStyle.Decimal };
            foreach (var t in top)
            {
                list.ListItems.Add(new ListItem(new Paragraph(new Run(
                    $"{t.BlendName} — {t.AverageRating:F1} ★ ({t.RatingCount} hodnocení)"))));
            }
            doc.Blocks.Add(list);
        }

        // Print
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
        {
            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            paginator.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
            dlg.PrintDocument(paginator, "Kávopíči — Přehled hodnocení");
        }
    }

    private static TableCell CreateCell(string text, bool bold = false, Brush? foreground = null)
    {
        var run = new Run(text);
        if (bold) run.FontWeight = FontWeights.Bold;
        if (foreground != null) run.Foreground = foreground;

        return new TableCell(new Paragraph(run) { Margin = new Thickness(4) })
        {
            BorderBrush = Silver,
            BorderThickness = new Thickness(0, 0, 0, 0.5),
            Padding = new Thickness(4, 2, 4, 2)
        };
    }
}
