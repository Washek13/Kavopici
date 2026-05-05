using Kavopici.Models;

namespace Kavopici.Services;

public interface IStatisticsService
{
    Task<List<BlendStatistics>> GetBlendStatisticsAsync();
    Task<List<Rating>> GetUserRatingHistoryAsync(int userId);
    Task<List<SessionWithRating>> GetUserSessionHistoryAsync(int userId);
    Task<List<SupplierStatistics>> GetSupplierStatisticsAsync();
    Task<List<UserStatistics>> GetUserStatisticsAsync();

    /// <summary>
    /// Returns a per-session average rating timeline for a blend (or its linked group),
    /// ordered chronologically (oldest → newest). Up to <paramref name="take"/> most recent points.
    /// </summary>
    Task<List<double>> GetBlendTrendAsync(int blendId, int take = 5);
}
