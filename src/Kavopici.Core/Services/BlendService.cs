using Kavopici.Data;
using Kavopici.Models;
using Kavopici.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kavopici.Services;

public class BlendService : IBlendService
{
    private readonly IDbContextFactory<KavopiciDbContext> _contextFactory;

    public BlendService(IDbContextFactory<KavopiciDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<CoffeeBlend>> GetActiveBlendsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CoffeeBlends
            .Where(b => b.IsActive)
            .Include(b => b.Supplier)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<CoffeeBlend> CreateBlendAsync(string name, string roaster, string? origin,
        RoastLevel roastLevel, int supplierId, int? weightGrams = null, decimal? priceCzk = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Název směsi nemůže být prázdný.");
        if (string.IsNullOrWhiteSpace(roaster))
            throw new ArgumentException("Název pražírny nemůže být prázdný.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var blend = new CoffeeBlend
        {
            Name = name.Trim(),
            Roaster = roaster.Trim(),
            Origin = origin?.Trim(),
            RoastLevel = roastLevel,
            SupplierId = supplierId,
            WeightGrams = weightGrams,
            PriceCzk = priceCzk,
            PricePerKg = CalculatePricePerKg(weightGrams, priceCzk),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.CoffeeBlends.Add(blend);
        await context.SaveChangesAsync();

        // Reload with supplier
        await context.Entry(blend).Reference(b => b.Supplier).LoadAsync();
        return blend;
    }

    public async Task<CoffeeBlend> UpdateBlendAsync(int blendId, string name, string roaster, string? origin,
        RoastLevel roastLevel, int supplierId, int? weightGrams, decimal? priceCzk)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Název směsi nemůže být prázdný.");
        if (string.IsNullOrWhiteSpace(roaster))
            throw new ArgumentException("Název pražírny nemůže být prázdný.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var blend = await context.CoffeeBlends.FindAsync(blendId)
            ?? throw new InvalidOperationException("Směs nebyla nalezena.");

        blend.Name = name.Trim();
        blend.Roaster = roaster.Trim();
        blend.Origin = origin?.Trim();
        blend.RoastLevel = roastLevel;
        blend.SupplierId = supplierId;
        blend.WeightGrams = weightGrams;
        blend.PriceCzk = priceCzk;
        blend.PricePerKg = CalculatePricePerKg(weightGrams, priceCzk);

        await context.SaveChangesAsync();

        await context.Entry(blend).Reference(b => b.Supplier).LoadAsync();
        return blend;
    }

    public async Task DeactivateBlendAsync(int blendId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var blend = await context.CoffeeBlends.FindAsync(blendId)
            ?? throw new InvalidOperationException("Směs nebyla nalezena.");

        blend.IsActive = false;
        await context.SaveChangesAsync();
    }

    public async Task<CoffeeBlend?> GetBlendByIdAsync(int blendId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CoffeeBlends
            .Include(b => b.Supplier)
            .FirstOrDefaultAsync(b => b.Id == blendId);
    }

    private static decimal? CalculatePricePerKg(int? weightGrams, decimal? priceCzk)
    {
        if (weightGrams is > 0 && priceCzk.HasValue)
            return Math.Round(priceCzk.Value / weightGrams.Value * 1000, 2);
        return null;
    }
}
