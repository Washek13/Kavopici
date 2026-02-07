using Kavopici.Models;
using Kavopici.Models.Enums;
using Kavopici.Services;
using Kavopici.Tests.Helpers;
using Xunit;

namespace Kavopici.Tests.Services;

public class RatingServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly RatingService _ratingService;
    private readonly UserService _userService;
    private readonly BlendService _blendService;
    private readonly SessionService _sessionService;

    public RatingServiceTests()
    {
        _factory = new TestDbContextFactory();
        _ratingService = new RatingService(_factory);
        _userService = new UserService(_factory);
        _blendService = new BlendService(_factory);
        _sessionService = new SessionService(_factory);
    }

    private async Task<(User user, TastingSession session)> SetupAsync()
    {
        var user = await _userService.CreateUserAsync("Test User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test Blend", "Test Roaster", "Origin",
            RoastLevel.Medium, user.Id);
        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id);
        return (user, session);
    }

    [Fact]
    public async Task SubmitRatingAsync_CreatesRating()
    {
        var (user, session) = await SetupAsync();

        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, "Great coffee!");

        Assert.Equal(4, rating.Stars);
        Assert.Equal("Great coffee!", rating.Comment);
    }

    [Fact]
    public async Task SubmitRatingAsync_DuplicateRating_Throws()
    {
        var (user, session) = await SetupAsync();

        await _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 4, null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 5, null));
    }

    [Fact]
    public async Task SubmitRatingAsync_InvalidStars_Throws()
    {
        var (user, session) = await SetupAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 0, null));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 6, null));
    }

    [Fact]
    public async Task GetUserRatingForSessionAsync_NoRating_ReturnsNull()
    {
        var (user, session) = await SetupAsync();

        var result = await _ratingService.GetUserRatingForSessionAsync(user.Id, session.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserRatingForSessionAsync_WithRating_ReturnsRating()
    {
        var (user, session) = await SetupAsync();
        await _ratingService.SubmitRatingAsync(session.BlendId, user.Id, session.Id, 3, "OK");

        var result = await _ratingService.GetUserRatingForSessionAsync(user.Id, session.Id);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Stars);
    }

    [Fact]
    public async Task UpdateRatingAsync_UpdatesStarsAndComment()
    {
        var (user, session) = await SetupAsync();
        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 3, "OK");

        var updated = await _ratingService.UpdateRatingAsync(rating.Id, 5, "Actually great!");

        Assert.Equal(5, updated.Stars);
        Assert.Equal("Actually great!", updated.Comment);
    }

    [Fact]
    public async Task SubmitRatingAsync_NullComment_StoresNull()
    {
        var (user, session) = await SetupAsync();

        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, null);

        Assert.Null(rating.Comment);
    }

    [Fact]
    public async Task SubmitRatingAsync_WhitespaceComment_StoresNull()
    {
        var (user, session) = await SetupAsync();

        var rating = await _ratingService.SubmitRatingAsync(
            session.BlendId, user.Id, session.Id, 4, "   ");

        Assert.Null(rating.Comment);
    }

    public void Dispose() => _factory.Dispose();
}
