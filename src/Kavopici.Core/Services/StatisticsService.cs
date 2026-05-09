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

        // Group by root ID: linked blends share one root, standalone blends are their own root
        var groups = blends.GroupBy(b => b.LinkedBlendId ?? b.Id);

        return groups.Select(g =>
        {
            var groupBlends = g.ToList();

            // Root blend is the one with LinkedBlendId == null, or the first in the group
            var rootBlend = groupBlends.FirstOrDefault(b => b.LinkedBlendId == null) ?? groupBlends[0];

            // Latest blend (most recently created) for price display
            var latestBlend = groupBlends.OrderByDescending(b => b.CreatedAt).First();

            // Aggregate all ratings across the group
            var ratings = groupBlends.SelectMany(b => b.Ratings).ToList();
            var distribution = new int[10];
            foreach (var r in ratings)
            {
                if (r.Stars >= 1 && r.Stars <= 10)
                    distribution[r.Stars - 1]++;
            }

            var averageRating = ratings.Count > 0 ? ratings.Average(r => r.Stars) : 0.0;

            double? controversyLevel = null;
            if (ratings.Count >= 2)
            {
                controversyLevel = ratings.Average(r => (r.Stars - averageRating) * (r.Stars - averageRating));
            }

            var pricePerKg = latestBlend.PricePerKg;

            decimal? pricePerformance = null;
            if (pricePerKg.HasValue && averageRating > 0)
            {
                pricePerformance = Math.Round(pricePerKg.Value / (decimal)averageRating, 0);
            }

            return new BlendStatistics(
                BlendId: rootBlend.Id,
                BlendName: rootBlend.Name,
                Roaster: rootBlend.Roaster,
                Origin: rootBlend.Origin,
                RoastLevel: rootBlend.RoastLevel,
                SupplierName: rootBlend.Supplier.Name,
                AverageRating: averageRating,
                RatingCount: ratings.Count,
                Distribution: distribution,
                PricePerKg: pricePerKg,
                ControversyLevel: controversyLevel,
                PricePerformance: pricePerformance,
                LinkedBlendCount: groupBlends.Count
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
                TotalDoses: g.Sum(s => s.DoseMultiplier),
                Last30DaysDoses: g.Where(s => s.Date >= cutoff).Sum(s => s.DoseMultiplier)
            ))
            .OrderByDescending(s => s.TotalDoses)
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

    public async Task<List<UserStatistics>> GetUserStatisticsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var users = await context.Users
            .Where(u => u.IsActive)
            .ToListAsync();

        var ratings = await context.Ratings
            .Include(r => r.RatingTastingNotes)
                .ThenInclude(rtn => rtn.TastingNote)
            .ToListAsync();

        var sessions = await context.TastingSessions
            .Where(s => s.IsActive)
            .ToListAsync();
        var totalSessions = sessions.Count;

        var blends = await context.CoffeeBlends
            .Where(b => b.IsActive)
            .Include(b => b.Ratings)
            .ToListAsync();

        return users.Select(user =>
        {
            var userRatings = ratings.Where(r => r.UserId == user.Id).ToList();
            var voteCount = userRatings.Count;
            var averageGiven = voteCount > 0 ? userRatings.Average(r => r.Stars) : 0.0;
            var participationRate = totalSessions > 0 ? (double)voteCount / totalSessions * 100.0 : 0.0;

            // Most-used tasting note
            string? favoriteTastingNote = userRatings
                .SelectMany(r => r.RatingTastingNotes)
                .GroupBy(rtn => rtn.TastingNote.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            // Supplied blends
            var suppliedBlends = blends.Where(b => b.SupplierId == user.Id).ToList();
            var suppliedBlendsCount = suppliedBlends.Count;

            // Price per star for supplied blends (only blends with price and at least one rating)
            var blendsWithPriceAndRatings = suppliedBlends
                .Where(b => b.PricePerKg.HasValue && b.Ratings.Count > 0)
                .ToList();

            decimal? suppliedAvgPricePerStar = null;
            if (blendsWithPriceAndRatings.Count > 0)
            {
                var pricePerStarValues = blendsWithPriceAndRatings
                    .Select(b => b.PricePerKg!.Value / (decimal)b.Ratings.Average(r => r.Stars))
                    .ToList();
                suppliedAvgPricePerStar = Math.Round(pricePerStarValues.Average(), 0);
            }

            // Voting consistency: population std dev (null if < 2 votes)
            double? votingConsistency = null;
            if (voteCount >= 2)
            {
                var variance = userRatings.Average(r => (r.Stars - averageGiven) * (r.Stars - averageGiven));
                votingConsistency = Math.Sqrt(variance);
            }

            // Cleanup duty stats
            var userCleanups = sessions.Where(s => s.CleanupPersonId == user.Id).ToList();
            var cleanupCount = userCleanups.Count;
            var resolvedCleanups = userCleanups.Where(s => s.CleanupCompleted.HasValue).ToList();
            double? cleanupReliability = resolvedCleanups.Count > 0
                ? resolvedCleanups.Count(s => s.CleanupCompleted == true) * 100.0 / resolvedCleanups.Count
                : null;

            return new UserStatistics(
                UserId: user.Id,
                UserName: user.Name,
                VoteCount: voteCount,
                AverageGiven: averageGiven,
                ParticipationRate: participationRate,
                FavoriteTastingNote: favoriteTastingNote,
                SuppliedBlendsCount: suppliedBlendsCount,
                SuppliedAvgPricePerStar: suppliedAvgPricePerStar,
                VotingConsistency: votingConsistency,
                CleanupCount: cleanupCount,
                CleanupReliability: cleanupReliability
            );
        })
        .OrderByDescending(u => u.VoteCount)
        .ToList();
    }

    public async Task<List<double>> GetBlendTrendAsync(int blendId, int take = 5)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Resolve linked-group blend IDs so the trend covers the full group.
        var rootId = await context.CoffeeBlends
            .Where(b => b.Id == blendId)
            .Select(b => b.LinkedBlendId ?? b.Id)
            .FirstOrDefaultAsync();
        if (rootId == 0) return new List<double>();

        var groupIds = await context.CoffeeBlends
            .Where(b => b.Id == rootId || b.LinkedBlendId == rootId)
            .Select(b => b.Id)
            .ToListAsync();

        // Per-session average across the group, ordered oldest-first.
        var perSession = await context.Ratings
            .Where(r => groupIds.Contains(r.BlendId))
            .GroupBy(r => new { r.SessionId, r.Session.Date })
            .Select(g => new { g.Key.Date, Avg = g.Average(x => (double)x.Stars) })
            .OrderBy(x => x.Date)
            .ToListAsync();

        if (perSession.Count == 0) return new List<double>();

        // Take the most recent N, preserving chronological order.
        return perSession
            .Skip(Math.Max(0, perSession.Count - take))
            .Select(x => x.Avg)
            .ToList();
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
