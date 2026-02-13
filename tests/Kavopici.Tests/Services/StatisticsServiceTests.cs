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
        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id);

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

        var session = await _sessionService.SetBlendOfTheDayAsync(blendA.Id);
        await _ratingService.SubmitRatingAsync(blendA.Id, user.Id, session.Id, 2, null);

        var session2 = await _sessionService.SetBlendOfTheDayAsync(blendB.Id);
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
        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id);
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
        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id);

        await _ratingService.SubmitRatingAsync(blend.Id, user1.Id, session.Id, 3, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user2.Id, session.Id, 3, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user3.Id, session.Id, 3, null);

        var stats = await _statisticsService.GetBlendStatisticsAsync();

        Assert.Single(stats);
        Assert.Equal(3.0, stats[0].AverageRating);
        Assert.Equal(3, stats[0].RatingCount);
        Assert.Equal(new[] { 0, 0, 3, 0, 0 }, stats[0].Distribution);
    }

    [Fact]
    public async Task GetUserRatingHistoryAsync_ReturnsWithBlendAndSession()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);
        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id);
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
        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id);

        await _ratingService.SubmitRatingAsync(blend.Id, user1.Id, session.Id, 4, null);
        await _ratingService.SubmitRatingAsync(blend.Id, user2.Id, session.Id, 3, null);

        var history = await _statisticsService.GetUserRatingHistoryAsync(user1.Id);

        Assert.Single(history);
        Assert.Equal(4, history[0].Stars);
    }

    public void Dispose() => _factory.Dispose();
}
