using Kavopici.Models;

namespace Kavopici.Services;

public interface ISessionService
{
    Task<TastingSession?> GetTodaySessionAsync();
    Task<TastingSession> SetBlendOfTheDayAsync(int blendId);
    Task<List<TastingSession>> GetSessionHistoryAsync();
}
