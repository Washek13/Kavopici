using Kavopici.Models;
using Kavopici.Models.Enums;

namespace Kavopici.Services;

public interface IRatingService
{
    Task<Rating?> GetUserRatingForSessionAsync(int userId, int sessionId);
    Task<Rating> SubmitRatingAsync(int blendId, int userId, int sessionId, int stars, string? comment);
    Task<Rating> UpdateRatingAsync(int ratingId, int stars, string? comment);

    /// <summary>
    /// Deletes a rating. Regular users may delete only their own ratings; admins may delete any rating.
    /// </summary>
    /// <exception cref="InvalidOperationException">Rating or requesting user not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Requesting user is not the author and not an admin.</exception>
    Task DeleteRatingAsync(int ratingId, int requestingUserId);
    Task<List<Rating>> GetRatingsForSessionAsync(int sessionId);
    Task<List<Rating>> GetRatingsForBlendAsync(int blendId);
    Task<List<Rating>> GetRatingsForBlendsAsync(IEnumerable<int> blendIds);
    Task<List<TastingNote>> GetAllTastingNotesAsync(Theme theme = Theme.Coffee);
    Task SetRatingNotesAsync(int ratingId, List<int> noteIds);
    Task<List<int>> GetRatingNoteIdsAsync(int ratingId);
}
