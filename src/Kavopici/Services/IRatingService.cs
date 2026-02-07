using Kavopici.Models;

namespace Kavopici.Services;

public interface IRatingService
{
    Task<Rating?> GetUserRatingForSessionAsync(int userId, int sessionId);
    Task<Rating> SubmitRatingAsync(int blendId, int userId, int sessionId, int stars, string? comment);
    Task<Rating> UpdateRatingAsync(int ratingId, int stars, string? comment);
    Task<List<Rating>> GetRatingsForSessionAsync(int sessionId);
    Task<List<Rating>> GetRatingsForBlendAsync(int blendId);
    Task<List<TastingNote>> GetAllTastingNotesAsync();
    Task SetRatingNotesAsync(int ratingId, List<int> noteIds);
    Task<List<int>> GetRatingNoteIdsAsync(int ratingId);
}
