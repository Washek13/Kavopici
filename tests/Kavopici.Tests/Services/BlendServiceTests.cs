using Kavopici.Models.Enums;
using Kavopici.Services;
using Kavopici.Tests.Helpers;
using Xunit;

namespace Kavopici.Tests.Services;

public class BlendServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly BlendService _blendService;
    private readonly UserService _userService;

    public BlendServiceTests()
    {
        _factory = new TestDbContextFactory();
        _blendService = new BlendService(_factory);
        _userService = new UserService(_factory);
    }

    private async Task<int> CreateSupplierAsync(string name = "Supplier")
    {
        var user = await _userService.CreateUserAsync(name, isAdmin: true);
        return user.Id;
    }

    [Fact]
    public async Task CreateBlendAsync_CreatesBlend()
    {
        var supplierId = await CreateSupplierAsync("Jan");

        var blend = await _blendService.CreateBlendAsync(
            "Ethiopia", "Doubleshot", "Sidamo", RoastLevel.Medium, supplierId);

        Assert.Equal("Ethiopia", blend.Name);
        Assert.Equal("Doubleshot", blend.Roaster);
        Assert.Equal("Sidamo", blend.Origin);
        Assert.Equal(RoastLevel.Medium, blend.RoastLevel);
        Assert.True(blend.IsActive);
        Assert.NotNull(blend.Supplier);
        Assert.Equal("Jan", blend.Supplier.Name);
    }

    [Fact]
    public async Task CreateBlendAsync_TrimsInputs()
    {
        var supplierId = await CreateSupplierAsync();

        var blend = await _blendService.CreateBlendAsync(
            "  Ethiopia  ", "  Doubleshot  ", "  Sidamo  ", RoastLevel.Light, supplierId);

        Assert.Equal("Ethiopia", blend.Name);
        Assert.Equal("Doubleshot", blend.Roaster);
        Assert.Equal("Sidamo", blend.Origin);
    }

    [Fact]
    public async Task CreateBlendAsync_NullOrigin_StoresNull()
    {
        var supplierId = await CreateSupplierAsync();

        var blend = await _blendService.CreateBlendAsync(
            "Test", "Roaster", null, RoastLevel.Medium, supplierId);

        Assert.Null(blend.Origin);
    }

    [Fact]
    public async Task CreateBlendAsync_EmptyName_Throws()
    {
        var supplierId = await CreateSupplierAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _blendService.CreateBlendAsync("", "Roaster", null, RoastLevel.Medium, supplierId));
    }

    [Fact]
    public async Task CreateBlendAsync_WhitespaceName_Throws()
    {
        var supplierId = await CreateSupplierAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _blendService.CreateBlendAsync("   ", "Roaster", null, RoastLevel.Medium, supplierId));
    }

    [Fact]
    public async Task CreateBlendAsync_EmptyRoaster_Throws()
    {
        var supplierId = await CreateSupplierAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _blendService.CreateBlendAsync("Name", "", null, RoastLevel.Medium, supplierId));
    }

    [Fact]
    public async Task CreateBlendAsync_WhitespaceRoaster_Throws()
    {
        var supplierId = await CreateSupplierAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _blendService.CreateBlendAsync("Name", "   ", null, RoastLevel.Medium, supplierId));
    }

    [Fact]
    public async Task GetActiveBlendsAsync_ReturnsOnlyActive_SortedByName()
    {
        var supplierId = await CreateSupplierAsync();
        var charlie = await _blendService.CreateBlendAsync("Charlie", "Roaster", null, RoastLevel.Medium, supplierId);
        var alpha = await _blendService.CreateBlendAsync("Alpha", "Roaster", null, RoastLevel.Light, supplierId);
        var zebra = await _blendService.CreateBlendAsync("Zebra", "Roaster", null, RoastLevel.Dark, supplierId);
        await _blendService.DeactivateBlendAsync(zebra.Id);

        var blends = await _blendService.GetActiveBlendsAsync();

        Assert.Equal(2, blends.Count);
        Assert.Equal("Alpha", blends[0].Name);
        Assert.Equal("Charlie", blends[1].Name);
        Assert.NotNull(blends[0].Supplier);
    }

    [Fact]
    public async Task GetActiveBlendsAsync_EmptyDb_ReturnsEmpty()
    {
        var blends = await _blendService.GetActiveBlendsAsync();

        Assert.Empty(blends);
    }

    [Fact]
    public async Task DeactivateBlendAsync_SetsInactive()
    {
        var supplierId = await CreateSupplierAsync();
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, supplierId);

        await _blendService.DeactivateBlendAsync(blend.Id);

        var active = await _blendService.GetActiveBlendsAsync();
        Assert.Empty(active);

        var deactivated = await _blendService.GetBlendByIdAsync(blend.Id);
        Assert.NotNull(deactivated);
        Assert.False(deactivated!.IsActive);
    }

    [Fact]
    public async Task DeactivateBlendAsync_NonExistentBlend_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _blendService.DeactivateBlendAsync(9999));
    }

    [Fact]
    public async Task GetBlendByIdAsync_ExistingBlend_ReturnsWithSupplier()
    {
        var supplierId = await CreateSupplierAsync("Jan");
        var blend = await _blendService.CreateBlendAsync("Test", "Roaster", null, RoastLevel.Medium, supplierId);

        var result = await _blendService.GetBlendByIdAsync(blend.Id);

        Assert.NotNull(result);
        Assert.Equal("Test", result!.Name);
        Assert.NotNull(result.Supplier);
        Assert.Equal("Jan", result.Supplier.Name);
    }

    [Fact]
    public async Task GetBlendByIdAsync_NonExistentBlend_ReturnsNull()
    {
        var result = await _blendService.GetBlendByIdAsync(9999);

        Assert.Null(result);
    }

    public void Dispose() => _factory.Dispose();
}
