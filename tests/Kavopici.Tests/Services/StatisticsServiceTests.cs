using Kavopici.Models;
using Kavopici.Models.Enums;
using Kavopici.Services;
using Kavopici.Tests.Helpers;
using Xunit;

namespace Kavopici.Tests.Services;

public class StatisticsServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly StatisticsService _statisticsService;
    private readonly UserService _userService;
    private readonly BlendService _blendService;
    private readonly SessionService _sessionService;
    private readonly RatingService _ratingService;

    public StatisticsServiceTests()
    {
        _factory = new TestDbContextFactory();
        _statisticsService = new StatisticsService(_factory);
        _userService = new UserService(_factory);
        _blendService = new BlendService(_factory);
        _sessionService = new SessionService(_factory);
        _ratingService = new RatingService(_factory);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_NoBlends_ReturnsEmpty()
    {
        var stats = await _statisticsService.GetBlendStatisticsAsync();
        Assert.Empty(stats);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_WithRatings_CalculatesCorrectly()
    {
        var user1 = await _userService.CreateUserAsync("User1", isAdmin: true);
        var user2 = await _userService.CreateUserAsync("User2");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user1.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await _ratingService.SubmitRatingAsync(blend.Id, user1.Id, session.Id, 4, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user2.Id, session.Id, 2, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        Assert.Equal(3.0, stats[0].AverageRating);
        Assert.Equal(2, stats[0].RatingCount);
        Assert.Equal(0, stats[0].Distribution[0]); // 1 star
        Assert.Equal(1, stats[0].Distribution[1]); // 2 stars
        Assert.Equal(0, stats[0].Distribution[2]); // 3 stars
        Assert.Equal(1, stats[0].Distribution[3]); // 4 stars
        Assert.Equal(0, stats[0].Distribution[4]); // 5 stars
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_InactiveBlend_NotIncluded()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);
        await _blendService.DeactivateBlendAsync(blend.Id);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Empty(stats);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_NoRatings_ReturnsZeroAverage()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        Assert.Equal(0, stats[0].AverageRating);
        Assert.Equal(0, stats[0].RatingCount);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_MultipleBlends_OrderedByAverageDesc()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blendA = await _blendService.CreateBlendAsync("BlendA", "Roaster", null, RoastLevel.Medium, user.Id);
        var blendB = await _blendService.CreateBlendAsync("BlendB", "Roaster", null, RoastLevel.Dark, user.Id);

        var session = await _sessionService.AddBlendOfTheDayAsync(blendA.Id);
        await _ratingService.SubmitRatingAsync(blendA.Id, user.Id, session.Id, 2, null);

        var session2 = await _sessionService.AddBlendOfTheDayAsync(blendB.Id);
        await _ratingService.SubmitRatingAsync(blendB.Id, user.Id, session2.Id, 5, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Equal(2, stats.Count);
        Assert.Equal("BlendB", stats[0].BlendName);
        Assert.Equal("BlendA", stats[1].BlendName);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_VerifiesAllRecordFields()
    {
        var user = await _userService.CreateUserAsync("Jan", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync(
            "Ethiopia Yirgacheffe", "Doubleshot", "Ethiopia", RoastLevel.Light, user.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _ratingService.SubmitRatingAsync(blend.Id, user.Id, session.Id, 4, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        var s = stats[0];
        Assert.Equal(blend.Id, s.BlendId);
        Assert.Equal("Ethiopia Yirgacheffe", s.BlendName);
        Assert.Equal("Doubleshot", s.Roaster);
        Assert.Equal("Ethiopia", s.Origin);
        Assert.Equal(RoastLevel.Light, s.RoastLevel);
        Assert.Equal("Jan", s.SupplierName);
        Assert.Equal(4.0, s.AverageRating);
        Assert.Equal(1, s.RatingCount);
        Assert.Equal(new[] { 0, 0, 0, 1, 0 }, s.Distribution);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_AllSameStars_DistributionCorrect()
    {
        var user1 = await _userService.CreateUserAsync("User1", isAdmin: true);
        var user2 = await _userService.CreateUserAsync("User2");
        var user3 = await _userService.CreateUserAsync("User3");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user1.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await _ratingService.SubmitRatingAsync(blend.Id, user1.Id, session.Id, 3, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user2.Id, session.Id, 3, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user3.Id, session.Id, 3, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        Assert.Equal(3.0, stats[0].AverageRating);
        Assert.Equal(3, stats[0].RatingCount);
        Assert.Equal(new[] { 0, 0, 3, 0, 0 }, stats[0].Distribution);
    }

    // --- ControversyLevel tests ---

    [Fact]
    public async Task GetBlendStatisticsAsync_NoRatings_ControversyLevelNull()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        Assert.Null(stats[0].ControversyLevel);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_SingleRating_ControversyLevelNull()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _ratingService.SubmitRatingAsync(blend.Id, user.Id, session.Id, 4, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        Assert.Null(stats[0].ControversyLevel);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_AllSameRatings_ControversyLevelZero()
    {
        var user1 = await _userService.CreateUserAsync("User1", isAdmin: true);
        var user2 = await _userService.CreateUserAsync("User2");
        var user3 = await _userService.CreateUserAsync("User3");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user1.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await _ratingService.SubmitRatingAsync(blend.Id, user1.Id, session.Id, 3, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user2.Id, session.Id, 3, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user3.Id, session.Id, 3, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        Assert.NotNull(stats[0].ControversyLevel);
        Assert.Equal(0.0, stats[0].ControversyLevel!.Value, precision: 5);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_MixedRatings_ControversyLevelCorrect()
    {
        var user1 = await _userService.CreateUserAsync("User1", isAdmin: true);
        var user2 = await _userService.CreateUserAsync("User2");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user1.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await _ratingService.SubmitRatingAsync(blend.Id, user1.Id, session.Id, 4, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user2.Id, session.Id, 2, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        // Mean = 3.0, Variance = ((4-3)^2 + (2-3)^2) / 2 = 1.0
        Assert.Single(stats);
        Assert.NotNull(stats[0].ControversyLevel);
        Assert.Equal(1.0, stats[0].ControversyLevel!.Value, precision: 5);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_PolarizedRatings_HighControversyLevel()
    {
        var user1 = await _userService.CreateUserAsync("User1", isAdmin: true);
        var user2 = await _userService.CreateUserAsync("User2");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user1.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await _ratingService.SubmitRatingAsync(blend.Id, user1.Id, session.Id, 1, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user2.Id, session.Id, 5, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        // Mean = 3.0, Variance = ((1-3)^2 + (5-3)^2) / 2 = 4.0
        Assert.Single(stats);
        Assert.NotNull(stats[0].ControversyLevel);
        Assert.Equal(4.0, stats[0].ControversyLevel!.Value, precision: 5);
    }

    // --- PricePerformance tests ---

    [Fact]
    public async Task GetBlendStatisticsAsync_NoPricePerKg_PricePerformanceNull()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _ratingService.SubmitRatingAsync(blend.Id, user.Id, session.Id, 4, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        Assert.Null(stats[0].PricePerformance);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_NoRatings_PricePerformanceNull()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id, 250, 200m);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        Assert.Null(stats[0].PricePerformance);
    }

    [Fact]
    public async Task GetBlendStatisticsAsync_WithPriceAndRatings_PricePerformanceCorrect()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        // 250g at 200 CZK = 800 CZK/kg
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id, 250, 200m);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _ratingService.SubmitRatingAsync(blend.Id, user.Id, session.Id, 4, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        // PricePerKg = 800, AverageRating = 4.0, PricePerformance = 800/4 = 200
        Assert.Single(stats);
        Assert.NotNull(stats[0].PricePerformance);
        Assert.Equal(200m, stats[0].PricePerformance!.Value);
    }

    // --- GetUserRatingHistoryAsync tests ---

    [Fact]
    public async Task GetUserRatingHistoryAsync_ReturnsWithBlendAndSession()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _ratingService.SubmitRatingAsync(blend.Id, user.Id, session.Id, 4, null);

        var history = await _statisticsService.GetUserRatingHistoryAsync(user.Id);

        Assert.Single(history);
        Assert.NotNull(history[0].Blend);
        Assert.NotNull(history[0].Session);
    }

    [Fact]
    public async Task GetUserRatingHistoryAsync_NoRatings_ReturnsEmpty()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);

        var history = await _statisticsService.GetUserRatingHistoryAsync(user.Id);

        Assert.Empty(history);
    }

    [Fact]
    public async Task GetUserRatingHistoryAsync_OnlySpecifiedUsersRatings()
    {
        var user1 = await _userService.CreateUserAsync("User1", isAdmin: true);
        var user2 = await _userService.CreateUserAsync("User2");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user1.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await _ratingService.SubmitRatingAsync(blend.Id, user1.Id, session.Id, 4, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user2.Id, session.Id, 3, null);

        var history = await _statisticsService.GetUserRatingHistoryAsync(user1.Id);

        Assert.Single(history);
        Assert.Equal(4, history[0].Stars);
    }

    // --- GetUserSessionHistoryAsync tests ---

    private async Task<TastingSession> CreatePastSessionAsync(int blendId, DateOnly date)
    {
        using var context = _factory.CreateDbContext();
        var session = new TastingSession
        {
            BlendId = blendId,
            Date = date,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.TastingSessions.Add(session);
        await context.SaveChangesAsync();
        return session;
    }

    [Fact]
    public async Task GetUserSessionHistoryAsync_NoSessions_ReturnsEmpty()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);

        var result = await _statisticsService.GetUserSessionHistoryAsync(user.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserSessionHistoryAsync_MixedRatedAndUnrated_ReturnsAll()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var twoDaysAgo = DateOnly.FromDateTime(DateTime.Today.AddDays(-2));
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var session1 = await CreatePastSessionAsync(blend.Id, twoDaysAgo);
        var session2 = await CreatePastSessionAsync(blend.Id, yesterday);

        // Rate only the first session
        await _ratingService.SubmitRatingAsync(blend.Id, user.Id, session1.Id, 4, "Good");

        var result = await _statisticsService.GetUserSessionHistoryAsync(user.Id);

        Assert.Equal(2, result.Count);

        var rated = result.First(r => r.Session.Id == session1.Id);
        Assert.True(rated.HasRated);
        Assert.NotNull(rated.UserRating);
        Assert.Equal(4, rated.UserRating!.Stars);

        var unrated = result.First(r => r.Session.Id == session2.Id);
        Assert.False(unrated.HasRated);
        Assert.Null(unrated.UserRating);
    }

    [Fact]
    public async Task GetUserSessionHistoryAsync_OrderedByDateDesc()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var threeDaysAgo = DateOnly.FromDateTime(DateTime.Today.AddDays(-3));
        var twoDaysAgo = DateOnly.FromDateTime(DateTime.Today.AddDays(-2));
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await CreatePastSessionAsync(blend.Id, twoDaysAgo);
        await CreatePastSessionAsync(blend.Id, threeDaysAgo);
        await CreatePastSessionAsync(blend.Id, yesterday);

        var result = await _statisticsService.GetUserSessionHistoryAsync(user.Id);

        Assert.Equal(3, result.Count);
        Assert.Equal(yesterday, result[0].Session.Date);
        Assert.Equal(twoDaysAgo, result[1].Session.Date);
        Assert.Equal(threeDaysAgo, result[2].Session.Date);
    }

    [Fact]
    public async Task GetUserSessionHistoryAsync_IncludesBlendDetails()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("TestBlend", "TestRoaster", "Origin", RoastLevel.Medium, user.Id);

        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await CreatePastSessionAsync(blend.Id, yesterday);

        var result = await _statisticsService.GetUserSessionHistoryAsync(user.Id);

        Assert.Single(result);
        Assert.NotNull(result[0].Session.Blend);
        Assert.Equal("TestBlend", result[0].Session.Blend.Name);
        Assert.NotNull(result[0].Session.Blend.Supplier);
        Assert.Equal("User", result[0].Session.Blend.Supplier.Name);
    }

    // --- GetSupplierStatisticsAsync tests ---

    [Fact]
    public async Task GetSupplierStatisticsAsync_NoSessions_ReturnsEmpty()
    {
        var result = await _statisticsService.GetSupplierStatisticsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSupplierStatisticsAsync_MultipleSuppliersMultipleSessions_CountsCorrectly()
    {
        var supplier1 = await _userService.CreateUserAsync("Supplier1", isAdmin: true);
        var supplier2 = await _userService.CreateUserAsync("Supplier2");

        var blend1 = await _blendService.CreateBlendAsync("Blend1", "Roaster", null, RoastLevel.Medium, supplier1.Id);
        var blend2 = await _blendService.CreateBlendAsync("Blend2", "Roaster", null, RoastLevel.Dark, supplier2.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var twoDaysAgo = DateOnly.FromDateTime(DateTime.Today.AddDays(-2));

        await CreatePastSessionAsync(blend1.Id, today);
        await CreatePastSessionAsync(blend1.Id, yesterday);
        await CreatePastSessionAsync(blend1.Id, twoDaysAgo);
        await CreatePastSessionAsync(blend2.Id, today);

        var result = await _statisticsService.GetSupplierStatisticsAsync();

        Assert.Equal(2, result.Count);

        var s1 = result.First(s => s.SupplierName == "Supplier1");
        Assert.Equal(3, s1.TotalSessionCount);
        Assert.Equal(3, s1.Last30DaysSessionCount);

        var s2 = result.First(s => s.SupplierName == "Supplier2");
        Assert.Equal(1, s2.TotalSessionCount);
        Assert.Equal(1, s2.Last30DaysSessionCount);
    }

    [Fact]
    public async Task GetSupplierStatisticsAsync_OldSessionsNotInLast30Days()
    {
        var supplier = await _userService.CreateUserAsync("Supplier", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Blend", "Roaster", null, RoastLevel.Medium, supplier.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var oldDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-60));

        await CreatePastSessionAsync(blend.Id, today);
        await CreatePastSessionAsync(blend.Id, oldDate);

        var result = await _statisticsService.GetSupplierStatisticsAsync();

        Assert.Single(result);
        Assert.Equal(2, result[0].TotalSessionCount);
        Assert.Equal(1, result[0].Last30DaysSessionCount);
    }

    [Fact]
    public async Task GetSupplierStatisticsAsync_OrderedByTotalDesc()
    {
        var supplier1 = await _userService.CreateUserAsync("SupplierA", isAdmin: true);
        var supplier2 = await _userService.CreateUserAsync("SupplierB");

        var blend1 = await _blendService.CreateBlendAsync("Blend1", "Roaster", null, RoastLevel.Medium, supplier1.Id);
        var blend2 = await _blendService.CreateBlendAsync("Blend2", "Roaster", null, RoastLevel.Dark, supplier2.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        // supplier2 has more sessions
        await CreatePastSessionAsync(blend1.Id, today);
        await CreatePastSessionAsync(blend2.Id, today);
        await CreatePastSessionAsync(blend2.Id, yesterday);

        var result = await _statisticsService.GetSupplierStatisticsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("SupplierB", result[0].SupplierName);
        Assert.Equal("SupplierA", result[1].SupplierName);
    }

    public void Dispose() => _factory.Dispose();
}
