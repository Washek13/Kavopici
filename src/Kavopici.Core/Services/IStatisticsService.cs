using Kavopici.Models;

namespace Kavopici.Services;

public interface IStatisticsService
{
    Task<List<BlendStatistics>> GetBlendStatisticsAsync();
    Task<List<Rating>> GetUserRatingHistoryAsync(int userId);
    Task<List<SessionWithRating>> GetUserSessionHistoryAsync(int userId);
}
