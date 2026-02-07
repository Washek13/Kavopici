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

    public void Dispose() => _factory.Dispose();
}
