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
    public async Task AddBlendOfTheDayAsync_SameBlendMultipleTimes_Coexist()
    {
        var user = await _userService.CreateUserAsync("User", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, user.Id);

        var session1 = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        var session2 = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        Assert.NotEqual(session1.Id, session2.Id);
        Assert.Equal(blend.Id, session1.BlendId);
        Assert.Equal(blend.Id, session2.BlendId);

        var todaySessions = await _sessionService.GetTodaySessionsAsync();
        Assert.Contains(todaySessions, s => s.Id == session1.Id);
        Assert.Contains(todaySessions, s => s.Id == session2.Id);
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

    // --- Cleanup person tests ---

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_SetsActiveUser()
    {
        var supplier = await _userService.CreateUserAsync("Supplier", isAdmin: true);
        var alice = await _userService.CreateUserAsync("Alice");
        var bob = await _userService.CreateUserAsync("Bob");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, supplier.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        var updated = await _sessionService.AssignRandomCleanupPersonAsync(session.Id);

        Assert.NotNull(updated.CleanupPersonId);
        Assert.Contains(updated.CleanupPersonId.Value, new[] { supplier.Id, alice.Id, bob.Id });
        Assert.NotNull(updated.CleanupPerson);
        Assert.Null(updated.CleanupCompleted);
    }

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_ExcludesInactiveUsers()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var bob = await _userService.CreateUserAsync("Bob");
        await _userService.DeactivateUserAsync(bob.Id);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        // Run several rolls to make sure inactive Bob is never selected
        for (int i = 0; i < 30; i++)
        {
            var updated = await _sessionService.AssignRandomCleanupPersonAsync(session.Id);
            Assert.Equal(alice.Id, updated.CleanupPersonId);
        }
    }

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_ThrowsWhenNoActiveUsers()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        // Force every user inactive directly via the context (the service blocks
        // deactivating the last admin, which we want to bypass for this test).
        using (var context = _factory.CreateDbContext())
        {
            foreach (var u in context.Users)
                u.IsActive = false;
            await context.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.AssignRandomCleanupPersonAsync(session.Id));
    }

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_ThrowsWhenSessionMissing()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.AssignRandomCleanupPersonAsync(999));
    }

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_ResetsCompletedToPending()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await _sessionService.AssignRandomCleanupPersonAsync(session.Id);
        await _sessionService.SetCleanupCompletedAsync(session.Id, true);

        var rerolled = await _sessionService.AssignRandomCleanupPersonAsync(session.Id);

        Assert.Null(rerolled.CleanupCompleted);
    }

    [Fact]
    public async Task SetCleanupPersonAsync_SetsSpecifiedUser()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var bob = await _userService.CreateUserAsync("Bob");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        var updated = await _sessionService.SetCleanupPersonAsync(session.Id, bob.Id);

        Assert.Equal(bob.Id, updated.CleanupPersonId);
        Assert.Equal("Bob", updated.CleanupPerson?.Name);
        Assert.Null(updated.CleanupCompleted);
    }

    [Fact]
    public async Task SetCleanupPersonAsync_ThrowsWhenUserInactive()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var bob = await _userService.CreateUserAsync("Bob");
        await _userService.DeactivateUserAsync(bob.Id);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.SetCleanupPersonAsync(session.Id, bob.Id));
    }

    [Fact]
    public async Task SetCleanupPersonAsync_ThrowsWhenUserMissing()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.SetCleanupPersonAsync(session.Id, 999));
    }

    [Fact]
    public async Task ClearCleanupPersonAsync_RemovesAssignment()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await _sessionService.SetCleanupPersonAsync(session.Id, alice.Id);
        await _sessionService.SetCleanupCompletedAsync(session.Id, true);

        var cleared = await _sessionService.ClearCleanupPersonAsync(session.Id);

        Assert.Null(cleared.CleanupPersonId);
        Assert.Null(cleared.CleanupCompleted);
    }

    [Fact]
    public async Task SetCleanupCompletedAsync_MarksDoneAndNotDoneAndPending()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _sessionService.SetCleanupPersonAsync(session.Id, alice.Id);

        var done = await _sessionService.SetCleanupCompletedAsync(session.Id, true);
        Assert.True(done.CleanupCompleted);

        var notDone = await _sessionService.SetCleanupCompletedAsync(session.Id, false);
        Assert.False(notDone.CleanupCompleted);

        var pending = await _sessionService.SetCleanupCompletedAsync(session.Id, null);
        Assert.Null(pending.CleanupCompleted);
    }

    [Fact]
    public async Task SetCleanupCompletedAsync_ThrowsWhenNoPersonAssigned()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.SetCleanupCompletedAsync(session.Id, true));
    }

    [Fact]
    public async Task GetTodaySessionsAsync_IncludesCleanupPerson()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);
        var session = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _sessionService.SetCleanupPersonAsync(session.Id, alice.Id);

        var sessions = await _sessionService.GetTodaySessionsAsync();

        Assert.Single(sessions);
        Assert.NotNull(sessions[0].CleanupPerson);
        Assert.Equal("Alice", sessions[0].CleanupPerson!.Name);
    }

    // Inserts a historical session bypassing AddBlendOfTheDayAsync so we can control Date/CreatedAt/Completed.
    private async Task<int> SeedHistoricalSessionAsync(int blendId, int? cleanupPersonId, bool? completed, DateOnly date, DateTime createdAt)
    {
        using var context = _factory.CreateDbContext();
        var session = new TastingSession
        {
            BlendId = blendId,
            Date = date,
            IsActive = true,
            CleanupPersonId = cleanupPersonId,
            CleanupCompleted = completed,
            CreatedAt = createdAt
        };
        context.TastingSessions.Add(session);
        await context.SaveChangesAsync();
        return session.Id;
    }

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_DoesNotPickSamePersonTwoInARow()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var bob = await _userService.CreateUserAsync("Bob");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);

        // Prior session assigned to Alice.
        var prior = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _sessionService.SetCleanupPersonAsync(prior.Id, alice.Id);

        // Roll a new session 30 times; should never pick Alice.
        var current = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        for (int i = 0; i < 30; i++)
        {
            var updated = await _sessionService.AssignRandomCleanupPersonAsync(current.Id);
            Assert.Equal(bob.Id, updated.CleanupPersonId);
        }
    }

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_FallsBackWhenOnlyOneActiveUser()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);

        var prior = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        await _sessionService.SetCleanupPersonAsync(prior.Id, alice.Id);

        var current = await _sessionService.AddBlendOfTheDayAsync(blend.Id);
        var updated = await _sessionService.AssignRandomCleanupPersonAsync(current.Id);

        Assert.Equal(alice.Id, updated.CleanupPersonId);
    }

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_BiasesAwayFromFrequentCleaners()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var bob = await _userService.CreateUserAsync("Bob");
        var carol = await _userService.CreateUserAsync("Carol");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);
        // Alice: 5 completed cleanups in the window.
        for (int i = 0; i < 5; i++)
            await SeedHistoricalSessionAsync(blend.Id, alice.Id, true, today.AddDays(-10), DateTime.UtcNow.AddDays(-10).AddSeconds(i));
        // Bob: most-recent prior session — gets excluded as "last picked".
        await SeedHistoricalSessionAsync(blend.Id, bob.Id, true, today.AddDays(-1), DateTime.UtcNow.AddDays(-1));

        var current = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        // Candidates: [Alice, Carol]. Weights: Alice=1/6, Carol=1.
        // P(Alice) ~14%, P(Carol) ~86%. With 200 rolls Carol picks should dominate.
        int aliceCount = 0, carolCount = 0, bobCount = 0;
        for (int i = 0; i < 200; i++)
        {
            var updated = await _sessionService.AssignRandomCleanupPersonAsync(current.Id);
            if (updated.CleanupPersonId == alice.Id) aliceCount++;
            else if (updated.CleanupPersonId == carol.Id) carolCount++;
            else if (updated.CleanupPersonId == bob.Id) bobCount++;
        }

        Assert.Equal(0, bobCount); // Bob is the last picked → excluded.
        Assert.True(carolCount > 140, $"Carol should be picked >70% of the time, got {carolCount}/200.");
        Assert.True(aliceCount < 60, $"Alice should be picked <30% of the time, got {aliceCount}/200.");
    }

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_IgnoresCleanupsOutside30DayWindow()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var bob = await _userService.CreateUserAsync("Bob");
        var carol = await _userService.CreateUserAsync("Carol");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);
        // Alice: 5 completed cleanups, but 60 days ago — outside the window.
        for (int i = 0; i < 5; i++)
            await SeedHistoricalSessionAsync(blend.Id, alice.Id, true, today.AddDays(-60), DateTime.UtcNow.AddDays(-60).AddSeconds(i));
        // Bob: most-recent prior session — excluded as "last picked".
        await SeedHistoricalSessionAsync(blend.Id, bob.Id, true, today.AddDays(-1), DateTime.UtcNow.AddDays(-1));

        var current = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        // Candidates: [Alice, Carol] with equal weights (Alice's old cleanups don't count).
        int aliceCount = 0, carolCount = 0;
        for (int i = 0; i < 200; i++)
        {
            var updated = await _sessionService.AssignRandomCleanupPersonAsync(current.Id);
            if (updated.CleanupPersonId == alice.Id) aliceCount++;
            else if (updated.CleanupPersonId == carol.Id) carolCount++;
        }

        // Each ~50%; loose bound 30-70% to avoid flakiness.
        Assert.InRange(aliceCount, 60, 140);
        Assert.InRange(carolCount, 60, 140);
    }

    [Fact]
    public async Task AssignRandomCleanupPersonAsync_DoesNotCountSkippedOrPendingCleanups()
    {
        var alice = await _userService.CreateUserAsync("Alice", isAdmin: true);
        var bob = await _userService.CreateUserAsync("Bob");
        var carol = await _userService.CreateUserAsync("Carol");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, alice.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);
        // Alice: 3 skipped, 2 pending — none should count toward weighting.
        for (int i = 0; i < 3; i++)
            await SeedHistoricalSessionAsync(blend.Id, alice.Id, false, today.AddDays(-10), DateTime.UtcNow.AddDays(-10).AddSeconds(i));
        for (int i = 0; i < 2; i++)
            await SeedHistoricalSessionAsync(blend.Id, alice.Id, null, today.AddDays(-9), DateTime.UtcNow.AddDays(-9).AddSeconds(i));
        // Bob: most-recent prior session — excluded as "last picked".
        await SeedHistoricalSessionAsync(blend.Id, bob.Id, true, today.AddDays(-1), DateTime.UtcNow.AddDays(-1));

        var current = await _sessionService.AddBlendOfTheDayAsync(blend.Id);

        // Candidates: [Alice, Carol] with equal weights.
        int aliceCount = 0, carolCount = 0;
        for (int i = 0; i < 200; i++)
        {
            var updated = await _sessionService.AssignRandomCleanupPersonAsync(current.Id);
            if (updated.CleanupPersonId == alice.Id) aliceCount++;
            else if (updated.CleanupPersonId == carol.Id) carolCount++;
        }

        Assert.InRange(aliceCount, 60, 140);
        Assert.InRange(carolCount, 60, 140);
    }

    public void Dispose() => _factory.Dispose();
}
