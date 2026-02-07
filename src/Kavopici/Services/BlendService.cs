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
        RoastLevel roastLevel, int supplierId)
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.CoffeeBlends.Add(blend);
        await context.SaveChangesAsync();

        // Reload with supplier
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
}
