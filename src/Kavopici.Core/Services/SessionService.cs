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

    public async Task<TastingSession?> GetTodaySessionAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TastingSessions
            .Where(s => s.Date == Today && s.IsActive)
            .Include(s => s.Blend)
                .ThenInclude(b => b.Supplier)
            .FirstOrDefaultAsync();
    }

    public async Task<TastingSession> SetBlendOfTheDayAsync(int blendId, string? comment = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Deactivate any existing session for today
        var existingSessions = await context.TastingSessions
            .Where(s => s.Date == Today && s.IsActive)
            .ToListAsync();

        foreach (var existing in existingSessions)
        {
            existing.IsActive = false;
        }

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
