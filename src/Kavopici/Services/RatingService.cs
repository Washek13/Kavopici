using Kavopici.Data;
using Kavopici.Models;
using Microsoft.EntityFrameworkCore;

namespace Kavopici.Services;

public class RatingService : IRatingService
{
    private readonly IDbContextFactory<KavopiciDbContext> _contextFactory;

    public RatingService(IDbContextFactory<KavopiciDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Rating?> GetUserRatingForSessionAsync(int userId, int sessionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.SessionId == sessionId);
    }

    public async Task<Rating> SubmitRatingAsync(int blendId, int userId, int sessionId, int stars, string? comment)
    {
        if (stars < 1 || stars > 5)
            throw new ArgumentException("Hodnocení musí být v rozmezí 1–5 hvězd.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.Ratings
            .AnyAsync(r => r.UserId == userId && r.SessionId == sessionId);
        if (existing)
            throw new InvalidOperationException("Tuto kávu jste již hodnotili.");

        var rating = new Rating
        {
            BlendId = blendId,
            UserId = userId,
            SessionId = sessionId,
            Stars = stars,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        context.Ratings.Add(rating);
        await context.SaveChangesAsync();
        return rating;
    }

    public async Task<Rating> UpdateRatingAsync(int ratingId, int stars, string? comment)
    {
        if (stars < 1 || stars > 5)
            throw new ArgumentException("Hodnocení musí být v rozmezí 1–5 hvězd.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var rating = await context.Ratings.FindAsync(ratingId)
            ?? throw new InvalidOperationException("Hodnocení nebylo nalezeno.");

        rating.Stars = stars;
        rating.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        await context.SaveChangesAsync();
        return rating;
    }

    public async Task<List<Rating>> GetRatingsForSessionAsync(int sessionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Ratings
            .Where(r => r.SessionId == sessionId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Rating>> GetRatingsForBlendAsync(int blendId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Ratings
            .Where(r => r.BlendId == blendId)
            .Include(r => r.User)
            .Include(r => r.Session)
            .Include(r => r.RatingTastingNotes)
                .ThenInclude(rtn => rtn.TastingNote)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TastingNote>> GetAllTastingNotesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TastingNotes.OrderBy(n => n.Name).ToListAsync();
    }

    public async Task SetRatingNotesAsync(int ratingId, List<int> noteIds)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.RatingTastingNotes
            .Where(rtn => rtn.RatingId == ratingId)
            .ToListAsync();
        context.RatingTastingNotes.RemoveRange(existing);

        foreach (var noteId in noteIds)
        {
            context.RatingTastingNotes.Add(new RatingTastingNote
            {
                RatingId = ratingId,
                TastingNoteId = noteId
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<int>> GetRatingNoteIdsAsync(int ratingId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.RatingTastingNotes
            .Where(rtn => rtn.RatingId == ratingId)
            .Select(rtn => rtn.TastingNoteId)
            .ToListAsync();
    }
}
