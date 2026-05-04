using Kavopici.Data;
using Kavopici.Models;
using Microsoft.EntityFrameworkCore;

namespace Kavopici.Services;

public class SessionService : ISessionService
{
    private readonly IDbContextFactory<KavopiciDbContext> _contextFactory;

    public SessionService(IDbContextFactory<KavopiciDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private static string? TrimComment(string? comment)
        => string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    public async Task<List<TastingSession>> GetTodaySessionsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TastingSessions
            .Where(s => s.Date == Today && s.IsActive)
            .Include(s => s.Blend)
                .ThenInclude(b => b.Supplier)
            .Include(s => s.CleanupPerson)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<TastingSession> AddBlendOfTheDayAsync(int blendId, string? comment = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var session = new TastingSession
        {
            BlendId = blendId,
            Date = Today,
            IsActive = true,
            Comment = TrimComment(comment),
            CreatedAt = DateTime.UtcNow
        };

        context.TastingSessions.Add(session);
        await context.SaveChangesAsync();

        // Reload with blend and supplier
        await context.Entry(session).Reference(s => s.Blend).LoadAsync();
        await context.Entry(session.Blend).Reference(b => b.Supplier).LoadAsync();

        return session;
    }

    public async Task RemoveBlendOfTheDayAsync(int sessionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var session = await context.TastingSessions.FindAsync(sessionId);

        if (session == null || !session.IsActive)
            throw new InvalidOperationException("Sezení nebylo nalezeno.");

        session.IsActive = false;
        await context.SaveChangesAsync();
    }

    public async Task<List<TastingSession>> GetSessionHistoryAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TastingSessions
            .Include(s => s.Blend)
                .ThenInclude(b => b.Supplier)
            .Include(s => s.CleanupPerson)
            .OrderByDescending(s => s.Date)
            .ToListAsync();
    }

    public async Task<TastingSession> AssignRandomCleanupPersonAsync(int sessionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var session = await context.TastingSessions.FindAsync(sessionId);
        if (session == null || !session.IsActive)
            throw new InvalidOperationException("Sezení nebylo nalezeno.");

        var activeUserIds = await context.Users
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        if (activeUserIds.Count == 0)
            throw new InvalidOperationException("Žádní aktivní uživatelé.");

        var pickedId = activeUserIds[Random.Shared.Next(activeUserIds.Count)];
        session.CleanupPersonId = pickedId;
        session.CleanupCompleted = null;
        await context.SaveChangesAsync();

        await context.Entry(session).Reference(s => s.CleanupPerson).LoadAsync();
        return session;
    }

    public async Task<TastingSession> SetCleanupPersonAsync(int sessionId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var session = await context.TastingSessions.FindAsync(sessionId);
        if (session == null || !session.IsActive)
            throw new InvalidOperationException("Sezení nebylo nalezeno.");

        var user = await context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
            throw new InvalidOperationException("Uživatel nebyl nalezen nebo není aktivní.");

        session.CleanupPersonId = userId;
        session.CleanupCompleted = null;
        await context.SaveChangesAsync();

        await context.Entry(session).Reference(s => s.CleanupPerson).LoadAsync();
        return session;
    }

    public async Task<TastingSession> ClearCleanupPersonAsync(int sessionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var session = await context.TastingSessions.FindAsync(sessionId);
        if (session == null || !session.IsActive)
            throw new InvalidOperationException("Sezení nebylo nalezeno.");

        session.CleanupPersonId = null;
        session.CleanupCompleted = null;
        await context.SaveChangesAsync();

        return session;
    }

    public async Task<TastingSession> SetCleanupCompletedAsync(int sessionId, bool? completed)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var session = await context.TastingSessions.FindAsync(sessionId);
        if (session == null || !session.IsActive)
            throw new InvalidOperationException("Sezení nebylo nalezeno.");

        if (session.CleanupPersonId == null)
            throw new InvalidOperationException("K sezení není přiřazena osoba pro úklid.");

        session.CleanupCompleted = completed;
        await context.SaveChangesAsync();

        await context.Entry(session).Reference(s => s.CleanupPerson).LoadAsync();
        return session;
    }
}
