using Kavopici.Models;

namespace Kavopici.Services;

public interface ISessionService
{
    Task<List<TastingSession>> GetTodaySessionsAsync();
    Task<List<TastingSession>> GetMostRecentUnratedSessionsAsync(int userId);
    Task<TastingSession> AddBlendOfTheDayAsync(int blendId, string? comment = null);
    Task RemoveBlendOfTheDayAsync(int sessionId);
    Task<List<TastingSession>> GetSessionHistoryAsync();
}
