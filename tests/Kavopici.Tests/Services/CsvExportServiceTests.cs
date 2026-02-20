using System.Text;
using Kavopici.Models.Enums;
using Kavopici.Services;
using Kavopici.Tests.Helpers;
using Xunit;

namespace Kavopici.Tests.Services;

public class CsvExportServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly CsvExportService _csvExportService;
    private readonly UserService _userService;
    private readonly BlendService _blendService;
    private readonly SessionService _sessionService;
    private readonly RatingService _ratingService;

    public CsvExportServiceTests()
    {
        _factory = new TestDbContextFactory();
        var statisticsService = new StatisticsService(_factory);
        _csvExportService = new CsvExportService(statisticsService);
        _userService = new UserService(_factory);
        _blendService = new BlendService(_factory);
        _sessionService = new SessionService(_factory);
        _ratingService = new RatingService(_factory);
    }

    private static string DecodeCsv(byte[] bytes)
    {
        var bom = new UTF8Encoding(true).GetPreamble();
        var content = bytes.AsSpan(bom.Length);
        return Encoding.UTF8.GetString(content);
    }

    [Fact]
    public async Task GenerateCsvBytesAsync_HasUtf8Bom()
    {
        var bytes = await _csvExportService.GenerateCsvBytesAsync();

        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    [Fact]
    public async Task GenerateCsvBytesAsync_EmptyDb_ReturnsHeaderOnly()
    {
        var bytes = await _csvExportService.GenerateCsvBytesAsync();
        var csv = DecodeCsv(bytes);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r')).ToArray();

        Assert.Single(lines);
        Assert.StartsWith("Název směsi;", lines[0]);
        Assert.Contains("5★", lines[0]);
    }

    [Fact]
    public async Task GenerateCsvBytesAsync_SingleBlendWithRating_CorrectRow()
    {
        var user = await _userService.CreateUserAsync("Admin", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync(
            "Test Blend", "Roaster", "Ethiopia", RoastLevel.Medium, user.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _ratingService.SubmitRatingAsync(blend.Id, user.Id, session.Id, 4, null);

        var bytes = await _csvExportService.GenerateCsvBytesAsync();
        var csv = DecodeCsv(bytes);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r')).ToArray();

        Assert.Equal(2, lines.Length);

        var columns = lines[1].Split(';');
        Assert.Equal("Test Blend", columns[0]);
        Assert.Equal("Roaster", columns[1]);
        Assert.Equal("Ethiopia", columns[2]);
        Assert.Equal("Střední", columns[3]);
        Assert.Equal("Admin", columns[4]);
        Assert.Equal("4.00", columns[5]);
        Assert.Equal("1", columns[6]);
        Assert.Equal("0", columns[7]);  // 1★
        Assert.Equal("0", columns[8]);  // 2★
        Assert.Equal("0", columns[9]);  // 3★
        Assert.Equal("1", columns[10]); // 4★
        Assert.Equal("0", columns[11]); // 5★
    }

    [Fact]
    public async Task GenerateCsvBytesAsync_AllRoastLevels_TranslatedCorrectly()
    {
        var user = await _userService.CreateUserAsync("Admin", isAdmin: true);
        await _blendService.CreateBlendAsync("A", "R", null, RoastLevel.Light, user.Id);
        await _blendService.CreateBlendAsync("B", "R", null, RoastLevel.MediumLight, user.Id);
        await _blendService.CreateBlendAsync("C", "R", null, RoastLevel.Medium, user.Id);
        await _blendService.CreateBlendAsync("D", "R", null, RoastLevel.MediumDark, user.Id);
        await _blendService.CreateBlendAsync("E", "R", null, RoastLevel.Dark, user.Id);

        var bytes = await _csvExportService.GenerateCsvBytesAsync();
        var csv = DecodeCsv(bytes);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r')).ToArray();

        var roastLevels = lines.Skip(1)
            .Select(l => l.Split(';')[3])
            .OrderBy(r => r)
            .ToList();

        Assert.Contains("Lehké", roastLevels);
        Assert.Contains("Středně lehké", roastLevels);
        Assert.Contains("Střední", roastLevels);
        Assert.Contains("Středně tmavé", roastLevels);
        Assert.Contains("Tmavé", roastLevels);
    }

    [Fact]
    public async Task GenerateCsvBytesAsync_EscapesSemicolonInValue()
    {
        var user = await _userService.CreateUserAsync("Admin", isAdmin: true);
        await _blendService.CreateBlendAsync("Blend;Special", "Roaster", null, RoastLevel.Medium, user.Id);

        var bytes = await _csvExportService.GenerateCsvBytesAsync();
        var csv = DecodeCsv(bytes);

        Assert.Contains("\"Blend;Special\"", csv);
    }

    [Fact]
    public async Task GenerateCsvBytesAsync_EscapesQuotesInValue()
    {
        var user = await _userService.CreateUserAsync("Admin", isAdmin: true);
        await _blendService.CreateBlendAsync("The \"Best\" Blend", "Roaster", null, RoastLevel.Medium, user.Id);

        var bytes = await _csvExportService.GenerateCsvBytesAsync();
        var csv = DecodeCsv(bytes);

        Assert.Contains("\"The \"\"Best\"\" Blend\"", csv);
    }

    [Fact]
    public async Task GenerateCsvBytesAsync_NullOrigin_EmptyString()
    {
        var user = await _userService.CreateUserAsync("Admin", isAdmin: true);
        await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var bytes = await _csvExportService.GenerateCsvBytesAsync();
        var csv = DecodeCsv(bytes);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r')).ToArray();

        var columns = lines[1].Split(';');
        Assert.Equal("", columns[2]);
    }

    [Fact]
    public async Task GenerateCsvBytesAsync_AverageFormattedWithTwoDecimals()
    {
        var user1 = await _userService.CreateUserAsync("User1", isAdmin: true);
        var user2 = await _userService.CreateUserAsync("User2");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user1.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await _ratingService.SubmitRatingAsync(blend.Id, user1.Id, session.Id, 4, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user2.Id, session.Id, 5, null);

        var bytes = await _csvExportService.GenerateCsvBytesAsync();
        var csv = DecodeCsv(bytes);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r')).ToArray();

        var columns = lines[1].Split(';');
        Assert.Equal("4.50", columns[5]);
    }

    public void Dispose() => _factory.Dispose();
}
