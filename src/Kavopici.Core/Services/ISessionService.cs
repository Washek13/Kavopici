using Kavopici.Models;

namespace Kavopici.Services;

public interface ISessionService
{
    Task<List<TastingSession>> GetTodaySessionsAsync();
    Task<TastingSession> AddBlendOfTheDayAsync(int blendId, string? comment = null, decimal doseMultiplier = 1.0m);
    Task RemoveBlendOfTheDayAsync(int sessionId);
    Task<TastingSession> SetSessionCommentAsync(int sessionId, string? comment);
    Task<TastingSession> SetSessionDoseMultiplierAsync(int sessionId, decimal doseMultiplier);
    Task<List<TastingSession>> GetSessionHistoryAsync();
    Task<TastingSession> AssignRandomCleanupPersonAsync(int sessionId);
    Task<TastingSession> SetCleanupPersonAsync(int sessionId, int userId);
    Task<TastingSession> ClearCleanupPersonAsync(int sessionId);
    Task<TastingSession> SetCleanupCompletedAsync(int sessionId, bool? completed);
}
