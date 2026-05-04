using Kavopici.Models;
using Kavopici.Models.Enums;

namespace Kavopici.Services;

public interface IRatingService
{
    Task<Rating?> GetUserRatingForSessionAsync(int userId, int sessionId);
    Task<Rating> SubmitRatingAsync(int blendId, int userId, int sessionId, int stars, string? comment);
    Task<Rating> UpdateRatingAsync(int ratingId, int stars, string? comment);
    Task<List<Rating>> GetRatingsForSessionAsync(int sessionId);
    Task<List<Rating>> GetRatingsForBlendAsync(int blendId);
    Task<List<Rating>> GetRatingsForBlendsAsync(IEnumerable<int> blendIds);
    Task<List<TastingNote>> GetAllTastingNotesAsync(Theme theme = Theme.Coffee);
    Task SetRatingNotesAsync(int ratingId, List<int> noteIds);
    Task<List<int>> GetRatingNoteIdsAsync(int ratingId);
}
