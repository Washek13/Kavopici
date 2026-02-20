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
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TastingSession>> GetMostRecentUnratedSessionsAsync(int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Find the most recent past date that has at least one unrated active session
        var mostRecentDate = await context.TastingSessions
            .Where(s => s.Date < Today && s.IsActive)
            .Where(s => !context.Ratings.Any(r => r.SessionId == s.Id && r.UserId == userId))
            .OrderByDescending(s => s.Date)
            .Select(s => (DateOnly?)s.Date)
            .FirstOrDefaultAsync();

        if (mostRecentDate == null)
            return new List<TastingSession>();

        // Return all unrated sessions from that date
        return await context.TastingSessions
            .Where(s => s.Date == mostRecentDate && s.IsActive)
            .Where(s => !context.Ratings.Any(r => r.SessionId == s.Id && r.UserId == userId))
            .Include(s => s.Blend)
                .ThenInclude(b => b.Supplier)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<TastingSession> AddBlendOfTheDayAsync(int blendId, string? comment = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Prevent adding the same blend twice for the same day
        var duplicate = await context.TastingSessions
            .AnyAsync(s => s.BlendId == blendId && s.Date == Today && s.IsActive);

        if (duplicate)
            throw new InvalidOperationException("Tato směs je již nastavena jako káva dne.");

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
            .OrderByDescending(s => s.Date)
            .ToListAsync();
    }
}
