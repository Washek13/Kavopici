using Kavopici.Models;

namespace Kavopici.Services;

public interface IUserService
{
    Task<List<User>> GetActiveUsersAsync();
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(string name, bool isAdmin = false);
    Task DeactivateUserAsync(int userId);
    Task ToggleAdminAsync(int userId);
    Task<bool> IsLastAdminAsync(int userId);
    Task<bool> HasAnyUsersAsync();
}
