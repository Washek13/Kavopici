using Kavopici.Models;
using Kavopici.Models.Enums;
using Kavopici.Services;
using Kavopici.Tests.Helpers;
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

        await _sessionService.SetBlendOfTheDayAsync(blend1.Id);
        var session2 = await _sessionService.SetBlendOfTheDayAsync(blend2.Id);

        var todaySession = await _sessionService.GetTodaySessionAsync();
        Assert.NotNull(todaySession);
        Assert.Equal(blend2.Id, todaySession!.BlendId);
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

    public void Dispose() => _factory.Dispose();
}
