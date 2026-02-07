using Kavopici.Models;

namespace Kavopici.Services;

public interface ISessionService
{
    Task<TastingSession?> GetTodaySessionAsync();
    Task<TastingSession> SetBlendOfTheDayAsync(int blendId, string? comment = null);
    Task<List<TastingSession>> GetSessionHistoryAsync();
}
