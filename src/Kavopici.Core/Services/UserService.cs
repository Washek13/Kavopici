using Kavopici.Data;
using Kavopici.Models;
using Microsoft.EntityFrameworkCore;

namespace Kavopici.Services;

public class UserService : IUserService
{
    private readonly IDbContextFactory<KavopiciDbContext> _contextFactory;

    public UserService(IDbContextFactory<KavopiciDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<User> CreateUserAsync(string name, bool isAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Jméno uživatele nemůže být prázdné.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var exists = await context.Users.AnyAsync(u => u.Name == name.Trim());
        if (exists)
            throw new InvalidOperationException($"Uživatel '{name.Trim()}' již existuje.");

        var user = new User
        {
            Name = name.Trim(),
            IsAdmin = isAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task DeactivateUserAsync(int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Uživatel nebyl nalezen.");

        if (user.IsAdmin && await IsLastAdminInternalAsync(context, userId))
            throw new InvalidOperationException("Nelze deaktivovat posledního administrátora.");

        user.IsActive = false;
        await context.SaveChangesAsync();
    }

    public async Task ToggleAdminAsync(int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Uživatel nebyl nalezen.");

        if (user.IsAdmin && await IsLastAdminInternalAsync(context, userId))
            throw new InvalidOperationException("Nelze odebrat práva poslednímu administrátorovi.");

        user.IsAdmin = !user.IsAdmin;
        await context.SaveChangesAsync();
    }

    public async Task<bool> IsLastAdminAsync(int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await IsLastAdminInternalAsync(context, userId);
    }

    public async Task<bool> HasAnyUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users.AnyAsync(u => u.IsActive);
    }

    private static async Task<bool> IsLastAdminInternalAsync(KavopiciDbContext context, int userId)
    {
        var adminCount = await context.Users
            .CountAsync(u => u.IsActive && u.IsAdmin);
        var user = await context.Users.FindAsync(userId);
        return user is { IsAdmin: true } && adminCount <= 1;
    }
}
