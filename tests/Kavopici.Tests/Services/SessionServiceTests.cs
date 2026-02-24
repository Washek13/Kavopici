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

    // --- GetTodaySessionsAsync tests ---

    [Fact]
    public async Task GetTodaySessionsAsync_NoSession_ReturnsEmpty()
    {
        var result = await _sessionService.GetTodaySessionsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTodaySessionsAsync_ReturnsSingleSession()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        var result = await _sessionService.GetTodaySessionsAsync();
        Assert.Single(result);
        Assert.Equal(blend.Id, result[0].BlendId);
    }

    [Fact]
    public async Task GetTodaySessionsAsync_ReturnsMultipleSessions()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend1 = await _blendService.CreateBlendAsync("Blend1", "Roaster", null, RoastLevel.Medium, user.Id);
        var blend2 = await _blendService.CreateBlendAsync("Blend2", "Roaster", null, RoastLevel.Dark, user.Id);

        await _sessionService.AddBlendOfTheDayAsync(blend1.Id, "Vzorek A");
        await _sessionService.AddBlendOfTheDayAsync(blend2.Id, "Vzorek B");

        var result = await _sessionService.GetTodaySessionsAsync();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTodaySessionsAsync_IncludesBlendAndSupplier()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", "Origin", RoastLevel.Medium, user.Id);

        await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        var result = await _sessionService.GetTodaySessionsAsync();

        Assert.Single(result);
        Assert.NotNull(result[0].Blend);
        Assert.Equal("Test", result[0].Blend.Name);
        Assert.NotNull(result[0].Blend.Supplier);
        Assert.Equal("User", result[0].Blend.Supplier.Name);
    }

    // --- AddBlendOfTheDayAsync tests ---

    [Fact]
    public async Task AddBlendOfTheDayAsync_CreatesSession()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        Assert.Equal(blend.Id, session.BlendId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), session.Date);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task AddBlendOfTheDayAsync_MultipleBlendsCoexist()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend1 = await _blendService.CreateBlendAsync("Blend1", "Roaster", null, RoastLevel.Medium, user.Id);
        var blend2 = await _blendService.CreateBlendAsync("Blend2", "Roaster", null, RoastLevel.Dark, user.Id);

        var session1 = await _sessionService.AddBlendOfTheDayAsync(blend1.Id);
        var session2 = await _sessionService.AddBlendOfTheDayAsync(blend2.Id);

        // Both sessions should be active
        using var context = _factory.CreateDbContext();
        var s1 = await context.TastingSessions.FirstAsync(s => s.Id == session1.Id);
        var s2 = await context.TastingSessions.FirstAsync(s => s.Id == session2.Id);
        Assert.True(s1.IsActive);
        Assert.True(s2.IsActive);

        var todaySessions = await _sessionService.GetTodaySessionsAsync();
        Assert.Equal(2, todaySessions.Count);
    }

    [Fact]
    public async Task AddBlendOfTheDayAsync_DuplicateBlend_Throws()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.AddBlendOfTheDayAsync(blend.Id));
    }

    [Fact]
    public async Task AddBlendOfTheDayAsync_WithComment_StoresComment()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id, "Dnes testujeme");

        Assert.Equal("Dnes testujeme", session.Comment);
    }

    [Fact]
    public async Task AddBlendOfTheDayAsync_WhitespaceComment_StoresNull()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id, "   ");

        Assert.Null(session.Comment);
    }

    [Fact]
    public async Task AddBlendOfTheDayAsync_CommentTrimmed()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id, "  Dobrá káva  ");

        Assert.Equal("Dobrá káva", session.Comment);
    }

    // --- RemoveBlendOfTheDayAsync tests ---

    [Fact]
    public async Task RemoveBlendOfTheDayAsync_DeactivatesSession()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _sessionService.RemoveBlendOfTheDayAsync(session.Id);

        using var context = _factory.CreateDbContext();
        var updated = await context.TastingSessions.FirstAsync(s => s.Id == session.Id);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task RemoveBlendOfTheDayAsync_DoesNotAffectOthers()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend1 = await _blendService.CreateBlendAsync("Blend1", "Roaster", null, RoastLevel.Medium, user.Id);
        var blend2 = await _blendService.CreateBlendAsync("Blend2", "Roaster", null, RoastLevel.Dark, user.Id);

        var session1 = await _sessionService.AddBlendOfTheDayAsync(blend1.Id);
        var session2 = await _sessionService.AddBlendOfTheDayAsync(blend2.Id);

        await _sessionService.RemoveBlendOfTheDayAsync(session1.Id);

        using var context = _factory.CreateDbContext();
        var s1 = await context.TastingSessions.FirstAsync(s => s.Id == session1.Id);
        var s2 = await context.TastingSessions.FirstAsync(s => s.Id == session2.Id);
        Assert.False(s1.IsActive);
        Assert.True(s2.IsActive);

        var todaySessions = await _sessionService.GetTodaySessionsAsync();
        Assert.Single(todaySessions);
        Assert.Equal(blend2.Id, todaySessions[0].BlendId);
    }

    [Fact]
    public async Task RemoveBlendOfTheDayAsync_NonExistentSession_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.RemoveBlendOfTheDayAsync(999));
    }

    [Fact]
    public async Task RemoveBlendOfTheDayAsync_AlreadyInactive_Throws()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _sessionService.RemoveBlendOfTheDayAsync(session.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.RemoveBlendOfTheDayAsync(session.Id));
    }

    // --- GetSessionHistoryAsync tests ---

    [Fact]
    public async Task GetSessionHistoryAsync_ReturnsAllSessions()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend1 = await _blendService.CreateBlendAsync("Blend1", "Roaster", null, RoastLevel.Medium, user.Id);
        var blend2 = await _blendService.CreateBlendAsync("Blend2", "Roaster", null, RoastLevel.Dark, user.Id);

        await _sessionService.AddBlendOfTheDayAsync(blend1.Id);
        await _sessionService.AddBlendOfTheDayAsync(blend2.Id);

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

    public void Dispose() => _factory.Dispose();
}
