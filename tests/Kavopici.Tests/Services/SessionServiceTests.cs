using Kavopici.Models;
using Kavopici.Models.Enums;
using Kavopici.Services;
using Kavopici.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kavopici.Tests.Services;

public class SessionServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SessionService _sessionService;
    private readonly UserService _userService;
    private readonly BlendService _blendService;

    public SessionServiceTests()
    {
        _factory = new TestDbContextFactory();
        _sessionService = new SessionService(_factory);
        _userService = new UserService(_factory);
        _blendService = new BlendService(_factory);
    }

    [Fact]
    public async Task GetTodaySessionAsync_NoSession_ReturnsNull()
    {
        var result = await _sessionService.GetTodaySessionAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetBlendOfTheDayAsync_CreatesSession()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id);

        Assert.Equal(blend.Id, session.BlendId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), session.Date);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task SetBlendOfTheDayAsync_DeactivatesPrevious()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend1 = await _blendService.CreateBlendAsync("Blend1", "Roaster", null, RoastLevel.Medium, user.Id);
        var blend2 = await _blendService.CreateBlendAsync("Blend2", "Roaster", null, RoastLevel.Dark, user.Id);

        var session1 = await _sessionService.SetBlendOfTheDayAsync(blend1.Id);
        var session2 = await _sessionService.SetBlendOfTheDayAsync(blend2.Id);

        var todaySession = await _sessionService.GetTodaySessionAsync();
        Assert.NotNull(todaySession);
        Assert.Equal(blend2.Id, todaySession!.BlendId);

        // Explicitly verify old session is deactivated
        using var context = _factory.CreateDbContext();
        var oldSession = await context.TastingSessions.FirstAsync(s => s.Id == session1.Id);
        Assert.False(oldSession.IsActive);
    }

    [Fact]
    public async Task GetTodaySessionAsync_IncludesBlendAndSupplier()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", "Origin", RoastLevel.Medium, user.Id);

        await _sessionService.SetBlendOfTheDayAsync(blend.Id);

        var session = await _sessionService.GetTodaySessionAsync();

        Assert.NotNull(session);
        Assert.NotNull(session!.Blend);
        Assert.Equal("Test", session.Blend.Name);
        Assert.NotNull(session.Blend.Supplier);
        Assert.Equal("User", session.Blend.Supplier.Name);
    }

    [Fact]
    public async Task SetBlendOfTheDayAsync_WithComment_StoresComment()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id, "Dnes testujeme");

        Assert.Equal("Dnes testujeme", session.Comment);
    }

    [Fact]
    public async Task SetBlendOfTheDayAsync_WhitespaceComment_StoresNull()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id, "   ");

        Assert.Null(session.Comment);
    }

    [Fact]
    public async Task SetBlendOfTheDayAsync_CommentTrimmed()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.SetBlendOfTheDayAsync(blend.Id, "  Dobrá káva  ");

        Assert.Equal("Dobrá káva", session.Comment);
    }

    [Fact]
    public async Task GetSessionHistoryAsync_ReturnsAllSessions()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend1 = await _blendService.CreateBlendAsync("Blend1", "Roaster", null, RoastLevel.Medium, user.Id);
        var blend2 = await _blendService.CreateBlendAsync("Blend2", "Roaster", null, RoastLevel.Dark, user.Id);

        await _sessionService.SetBlendOfTheDayAsync(blend1.Id);
        await _sessionService.SetBlendOfTheDayAsync(blend2.Id);

        var history = await _sessionService.GetSessionHistoryAsync();

        Assert.Equal(2, history.Count);
        Assert.All(history, s =>
        {
            Assert.NotNull(s.Blend);
            Assert.NotNull(s.Blend.Supplier);
        });
    }

    [Fact]
    public async Task GetSessionHistoryAsync_EmptyDb_ReturnsEmpty()
    {
        var history = await _sessionService.GetSessionHistoryAsync();

        Assert.Empty(history);
    }

    // --- GetMostRecentUnratedSessionAsync tests ---

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
    public async Task GetMostRecentUnratedSessionAsync_NoSessions_ReturnsNull()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);

        var result = await _sessionService.GetMostRecentUnratedSessionAsync(user.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMostRecentUnratedSessionAsync_AllRated_ReturnsNull()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);
        var ratingService = new RatingService(_factory);

        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var session = await CreatePastSessionAsync(blend.Id, yesterday);

        await ratingService.SubmitRatingAsync(blend.Id, user.Id, session.Id, 4, null);

        var result = await _sessionService.GetMostRecentUnratedSessionAsync(user.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMostRecentUnratedSessionAsync_HasUnrated_ReturnsMostRecent()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var twoDaysAgo = DateOnly.FromDateTime(DateTime.Today.AddDays(-2));
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await CreatePastSessionAsync(blend.Id, twoDaysAgo);
        await CreatePastSessionAsync(blend.Id, yesterday);

        var result = await _sessionService.GetMostRecentUnratedSessionAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(yesterday, result!.Date);
    }

    [Fact]
    public async Task GetMostRecentUnratedSessionAsync_IgnoresTodaySession()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        // Create today's session — should be ignored
        await _sessionService.SetBlendOfTheDayAsync(blend.Id);

        var result = await _sessionService.GetMostRecentUnratedSessionAsync(user.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMostRecentUnratedSessionAsync_IncludesBlendAndSupplier()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("TestBlend", "TestRoaster", "Origin", RoastLevel.Medium, user.Id);

        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await CreatePastSessionAsync(blend.Id, yesterday);

        var result = await _sessionService.GetMostRecentUnratedSessionAsync(user.Id);

        Assert.NotNull(result);
        Assert.NotNull(result!.Blend);
        Assert.Equal("TestBlend", result.Blend.Name);
        Assert.NotNull(result.Blend.Supplier);
        Assert.Equal("User", result.Blend.Supplier.Name);
    }

    public void Dispose() => _factory.Dispose();
}
