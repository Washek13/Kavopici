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

    [Fact]
    public async Task CreateBlendAsync_WithPriceAndWeight_CalculatesPricePerKg()
    {
        var supplierId = await CreateSupplierAsync();

        var blend = await _blendService.CreateBlendAsync(
            "Test", "Roaster", null, RoastLevel.Medium, supplierId, weightGrams: 250, priceCzk: 350m);

        Assert.Equal(250, blend.WeightGrams);
        Assert.Equal(350m, blend.PriceCzk);
        Assert.Equal(1400m, blend.PricePerKg);
    }

    [Fact]
    public async Task CreateBlendAsync_WithoutPrice_PricePerKgIsNull()
    {
        var supplierId = await CreateSupplierAsync();

        var blend = await _blendService.CreateBlendAsync(
            "Test", "Roaster", null, RoastLevel.Medium, supplierId);

        Assert.Null(blend.WeightGrams);
        Assert.Null(blend.PriceCzk);
        Assert.Null(blend.PricePerKg);
    }

    [Fact]
    public async Task CreateBlendAsync_ZeroWeight_PricePerKgIsNull()
    {
        var supplierId = await CreateSupplierAsync();

        var blend = await _blendService.CreateBlendAsync(
            "Test", "Roaster", null, RoastLevel.Medium, supplierId, weightGrams: 0, priceCzk: 350m);

        Assert.Null(blend.PricePerKg);
    }

    [Fact]
    public async Task UpdateBlendAsync_UpdatesAllFields()
    {
        var supplier1Id = await CreateSupplierAsync("Supplier1");
        var supplier2Id = await CreateSupplierAsync("Supplier2");
        var blend = await _blendService.CreateBlendAsync(
            "Original", "OldRoaster", "Brazil", RoastLevel.Light, supplier1Id);

        var updated = await _blendService.UpdateBlendAsync(blend.Id,
            "Updated", "NewRoaster", "Ethiopia", RoastLevel.Dark, supplier2Id, 500, 600m);

        Assert.Equal("Updated", updated.Name);
        Assert.Equal("NewRoaster", updated.Roaster);
        Assert.Equal("Ethiopia", updated.Origin);
        Assert.Equal(RoastLevel.Dark, updated.RoastLevel);
        Assert.Equal(supplier2Id, updated.SupplierId);
        Assert.Equal(500, updated.WeightGrams);
        Assert.Equal(600m, updated.PriceCzk);
        Assert.Equal(1200m, updated.PricePerKg);
        Assert.NotNull(updated.Supplier);
        Assert.Equal("Supplier2", updated.Supplier.Name);
    }

    [Fact]
    public async Task UpdateBlendAsync_EmptyName_Throws()
    {
        var supplierId = await CreateSupplierAsync();
        var blend = await _blendService.CreateBlendAsync(
            "Test", "Roaster", null, RoastLevel.Medium, supplierId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _blendService.UpdateBlendAsync(blend.Id, "", "Roaster", null, RoastLevel.Medium, supplierId, null, null));
    }

    [Fact]
    public async Task UpdateBlendAsync_EmptyRoaster_Throws()
    {
        var supplierId = await CreateSupplierAsync();
        var blend = await _blendService.CreateBlendAsync(
            "Test", "Roaster", null, RoastLevel.Medium, supplierId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _blendService.UpdateBlendAsync(blend.Id, "Test", "", null, RoastLevel.Medium, supplierId, null, null));
    }

    [Fact]
    public async Task UpdateBlendAsync_NonExistentBlend_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _blendService.UpdateBlendAsync(9999, "Test", "Roaster", null, RoastLevel.Medium, 1, null, null));
    }

    [Fact]
    public async Task UpdateBlendAsync_RecalculatesPricePerKg()
    {
        var supplierId = await CreateSupplierAsync();
        var blend = await _blendService.CreateBlendAsync(
            "Test", "Roaster", null, RoastLevel.Medium, supplierId, weightGrams: 250, priceCzk: 350m);
        Assert.Equal(1400m, blend.PricePerKg);

        var updated = await _blendService.UpdateBlendAsync(blend.Id,
            "Test", "Roaster", null, RoastLevel.Medium, supplierId, 1000, 500m);

        Assert.Equal(500m, updated.PricePerKg);
    }

    public void Dispose() => _factory.Dispose();
}
