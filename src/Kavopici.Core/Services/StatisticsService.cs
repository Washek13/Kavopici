using Kavopici.Data;
using Kavopici.Models;
using Microsoft.EntityFrameworkCore;

namespace Kavopici.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IDbContextFactory<KavopiciDbContext> _contextFactory;

    public StatisticsService(IDbContextFactory<KavopiciDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<BlendStatistics>> GetBlendStatisticsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var blends = await context.CoffeeBlends
            .Where(b => b.IsActive)
            .Include(b => b.Supplier)
            .Include(b => b.Ratings)
            .ToListAsync();

        return blends.Select(b =>
        {
            var ratings = b.Ratings.ToList();
            var distribution = new int[5];
            foreach (var r in ratings)
            {
                if (r.Stars >= 1 && r.Stars <= 5)
                    distribution[r.Stars - 1]++;
            }

            return new BlendStatistics(
                BlendId: b.Id,
                BlendName: b.Name,
                Roaster: b.Roaster,
                Origin: b.Origin,
                RoastLevel: b.RoastLevel,
                SupplierName: b.Supplier.Name,
                AverageRating: ratings.Count > 0 ? ratings.Average(r => r.Stars) : 0,
                RatingCount: ratings.Count,
                Distribution: distribution,
                PricePerKg: b.PricePerKg
            );
        })
        .OrderByDescending(s => s.AverageRating)
        .ToList();
    }

    public async Task<List<SupplierStatistics>> GetSupplierStatisticsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var sessions = await context.TastingSessions
            .Where(s => s.IsActive)
            .Include(s => s.Blend)
                .ThenInclude(b => b.Supplier)
            .ToListAsync();

        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));

        return sessions
            .GroupBy(s => new { s.Blend.SupplierId, SupplierName = s.Blend.Supplier.Name })
            .Select(g => new SupplierStatistics(
                SupplierId: g.Key.SupplierId,
                SupplierName: g.Key.SupplierName,
                TotalSessionCount: g.Count(),
                Last30DaysSessionCount: g.Count(s => s.Date >= cutoff)
            ))
            .OrderByDescending(s => s.TotalSessionCount)
            .ToList();
    }

    public async Task<List<Rating>> GetUserRatingHistoryAsync(int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Ratings
            .Where(r => r.UserId == userId)
            .Include(r => r.Blend)
            .Include(r => r.Session)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SessionWithRating>> GetUserSessionHistoryAsync(int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var sessions = await context.TastingSessions
            .Where(s => s.IsActive)
            .Include(s => s.Blend)
                .ThenInclude(b => b.Supplier)
            .OrderByDescending(s => s.Date)
            .ToListAsync();

        var sessionIds = sessions.Select(s => s.Id).ToList();

        var userRatings = await context.Ratings
            .Where(r => r.UserId == userId && sessionIds.Contains(r.SessionId))
            .Include(r => r.RatingTastingNotes)
                .ThenInclude(rtn => rtn.TastingNote)
            .ToDictionaryAsync(r => r.SessionId);

        return sessions.Select(s =>
        {
            userRatings.TryGetValue(s.Id, out var rating);
            return new SessionWithRating(s, rating, rating != null);
        }).ToList();
    }
}
