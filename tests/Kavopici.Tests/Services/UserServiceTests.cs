using Kavopici.Services;
using Kavopici.Tests.Helpers;
using Xunit;

namespace Kavopici.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _factory = new TestDbContextFactory();
        _service = new UserService(_factory);
    }

    [Fact]
    public async Task CreateUserAsync_CreatesUser()
    {
        var user = await _service.CreateUserAsync("Test User");

        Assert.Equal("Test User", user.Name);
        Assert.False(user.IsAdmin);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task CreateUserAsync_WithAdmin_SetsAdminFlag()
    {
        var user = await _service.CreateUserAsync("Admin", isAdmin: true);

        Assert.True(user.IsAdmin);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateName_Throws()
    {
        await _service.CreateUserAsync("Duplicate");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateUserAsync("Duplicate"));
    }

    [Fact]
    public async Task CreateUserAsync_EmptyName_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateUserAsync(""));
    }

    [Fact]
    public async Task GetActiveUsersAsync_ReturnsOnlyActive()
    {
        var user1 = await _service.CreateUserAsync("Active User", isAdmin: true);
        var user2 = await _service.CreateUserAsync("Inactive User");
        await _service.DeactivateUserAsync(user2.Id);

        var active = await _service.GetActiveUsersAsync();

        Assert.Single(active);
        Assert.Equal("Active User", active[0].Name);
    }

    [Fact]
    public async Task DeactivateUserAsync_SetsInactive()
    {
        await _service.CreateUserAsync("Admin", isAdmin: true);
        var user = await _service.CreateUserAsync("User");

        await _service.DeactivateUserAsync(user.Id);

        var active = await _service.GetActiveUsersAsync();
        Assert.DoesNotContain(active, u => u.Name == "User");
    }

    [Fact]
    public async Task DeactivateUserAsync_LastAdmin_Throws()
    {
        var admin = await _service.CreateUserAsync("Only Admin", isAdmin: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeactivateUserAsync(admin.Id));
    }

    [Fact]
    public async Task ToggleAdminAsync_TogglesFlag()
    {
        var admin = await _service.CreateUserAsync("Admin1", isAdmin: true);
        var user = await _service.CreateUserAsync("User");

        await _service.ToggleAdminAsync(user.Id);

        var users = await _service.GetAllUsersAsync();
        var toggled = users.First(u => u.Id == user.Id);
        Assert.True(toggled.IsAdmin);
    }

    [Fact]
    public async Task ToggleAdminAsync_LastAdmin_Throws()
    {
        var admin = await _service.CreateUserAsync("Only Admin", isAdmin: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ToggleAdminAsync(admin.Id));
    }

    [Fact]
    public async Task HasAnyUsersAsync_EmptyDb_ReturnsFalse()
    {
        var result = await _service.HasAnyUsersAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task HasAnyUsersAsync_WithUsers_ReturnsTrue()
    {
        await _service.CreateUserAsync("User");
        var result = await _service.HasAnyUsersAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task CreateUserAsync_WhitespaceOnlyName_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateUserAsync("   "));
    }

    [Fact]
    public async Task CreateUserAsync_TrimsName()
    {
        var user = await _service.CreateUserAsync("  Test User  ");

        Assert.Equal("Test User", user.Name);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateTrimmedName_Throws()
    {
        await _service.CreateUserAsync("Test");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateUserAsync("  Test  "));
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsActiveAndInactive()
    {
        var admin = await _service.CreateUserAsync("Admin", isAdmin: true);
        var user = await _service.CreateUserAsync("User");
        await _service.DeactivateUserAsync(user.Id);

        var all = await _service.GetAllUsersAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetAllUsersAsync_OrderedByName()
    {
        await _service.CreateUserAsync("Zuzka", isAdmin: true);
        await _service.CreateUserAsync("Adam");
        await _service.CreateUserAsync("Martin");

        var all = await _service.GetAllUsersAsync();

        Assert.Equal("Adam", all[0].Name);
        Assert.Equal("Martin", all[1].Name);
        Assert.Equal("Zuzka", all[2].Name);
    }

    [Fact]
    public async Task IsLastAdminAsync_SingleAdmin_ReturnsTrue()
    {
        var admin = await _service.CreateUserAsync("Admin", isAdmin: true);

        var result = await _service.IsLastAdminAsync(admin.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task IsLastAdminAsync_MultipleAdmins_ReturnsFalse()
    {
        var admin1 = await _service.CreateUserAsync("Admin1", isAdmin: true);
        var admin2 = await _service.CreateUserAsync("Admin2", isAdmin: true);

        var result = await _service.IsLastAdminAsync(admin1.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task IsLastAdminAsync_NonAdmin_ReturnsFalse()
    {
        await _service.CreateUserAsync("Admin", isAdmin: true);
        var user = await _service.CreateUserAsync("User");

        var result = await _service.IsLastAdminAsync(user.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task DeactivateUserAsync_NonExistentUser_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeactivateUserAsync(9999));
    }

    [Fact]
    public async Task ToggleAdminAsync_NonExistentUser_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ToggleAdminAsync(9999));
    }

    public void Dispose() => _factory.Dispose();
}
