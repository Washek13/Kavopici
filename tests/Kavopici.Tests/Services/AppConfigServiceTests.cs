using Kavopici.Models.Enums;
using Kavopici.Services;
using Kavopici.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kavopici.Tests.Services;

public class AppConfigServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly AppConfigService _service;

    public AppConfigServiceTests()
    {
        _factory = new TestDbContextFactory();
        _service = new AppConfigService(_factory);
    }

    [Fact]
    public async Task GetOrNullAsync_FreshDb_ReturnsNull()
    {
        // EnsureCreated seeds tasting notes via HasData but does NOT insert AppConfig —
        // that row is created at runtime, so a fresh DB has no AppConfig yet.
        var result = await _service.GetOrNullAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_Coffee_InsertsConfigAndKeepsCoffeeNotes()
    {
        var config = await _service.CreateAsync(Theme.Coffee);

        Assert.Equal(Theme.Coffee, config.Theme);

        await using var db = _factory.CreateDbContext();
        var coffeeNotes = await db.TastingNotes.Where(n => n.Theme == Theme.Coffee).ToListAsync();
        var teaNotes = await db.TastingNotes.Where(n => n.Theme == Theme.Tea).ToListAsync();

        Assert.Equal(8, coffeeNotes.Count);
        Assert.Empty(teaNotes); // no tea notes seeded for a coffee DB
    }

    [Fact]
    public async Task CreateAsync_Tea_InsertsConfigAndSeedsTeaNotes()
    {
        var config = await _service.CreateAsync(Theme.Tea);

        Assert.Equal(Theme.Tea, config.Theme);

        await using var db = _factory.CreateDbContext();
        var teaNotes = await db.TastingNotes.Where(n => n.Theme == Theme.Tea).ToListAsync();

        Assert.Equal(8, teaNotes.Count);
        var teaNames = teaNotes.Select(n => n.Name).ToList();
        Assert.Contains("Květinová", teaNames);
        Assert.Contains("Travnatá", teaNames);
        Assert.Contains("Sladká", teaNames);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCall_Throws()
    {
        await _service.CreateAsync(Theme.Coffee);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(Theme.Tea));
    }

    [Fact]
    public async Task GetThemeAsync_FreshDb_LazyInsertsCoffeeAndReturnsCoffee()
    {
        // Mirrors the legacy-database upgrade path: existing DBs with users but
        // no AppConfig row should auto-default to Coffee on first read.
        var theme = await _service.GetThemeAsync();

        Assert.Equal(Theme.Coffee, theme);

        var config = await _service.GetOrNullAsync();
        Assert.NotNull(config);
        Assert.Equal(Theme.Coffee, config!.Theme);
    }

    [Fact]
    public async Task GetThemeAsync_AfterCreate_ReturnsStoredTheme()
    {
        await _service.CreateAsync(Theme.Tea);

        var theme = await _service.GetThemeAsync();

        Assert.Equal(Theme.Tea, theme);
    }

    [Fact]
    public async Task CreateAsync_Tea_TeaNotesUseIdsAfterCoffeeRange()
    {
        // Coffee notes occupy IDs 1-8 from HasData seed; tea notes inserted
        // at runtime should land in IDs 9+ which the resx file expects.
        await _service.CreateAsync(Theme.Tea);

        await using var db = _factory.CreateDbContext();
        var teaNoteIds = await db.TastingNotes
            .Where(n => n.Theme == Theme.Tea)
            .Select(n => n.Id)
            .OrderBy(id => id)
            .ToListAsync();

        Assert.All(teaNoteIds, id => Assert.True(id >= 9, $"Expected tea note id >= 9 but got {id}"));
    }

    public void Dispose() => _factory.Dispose();
}
