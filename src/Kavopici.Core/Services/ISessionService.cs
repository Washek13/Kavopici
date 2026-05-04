using Kavopici.Models;

namespace Kavopici.Services;

public interface ISessionService
{
    Task<List<TastingSession>> GetTodaySessionsAsync();
    Task<TastingSession> AddBlendOfTheDayAsync(int blendId, string? comment = null);
    Task RemoveBlendOfTheDayAsync(int sessionId);
    Task<List<TastingSession>> GetSessionHistoryAsync();
    Task<TastingSession> AssignRandomCleanupPersonAsync(int sessionId);
    Task<TastingSession> SetCleanupPersonAsync(int sessionId, int userId);
    Task<TastingSession> ClearCleanupPersonAsync(int sessionId);
    Task<TastingSession> SetCleanupCompletedAsync(int sessionId, bool? completed);
}
