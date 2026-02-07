using Kavopici.Models;
using Kavopici.Models.Enums;

namespace Kavopici.Services;

public interface IBlendService
{
    Task<List<CoffeeBlend>> GetActiveBlendsAsync();
    Task<CoffeeBlend> CreateBlendAsync(string name, string roaster, string? origin,
        RoastLevel roastLevel, int supplierId);
    Task DeactivateBlendAsync(int blendId);
    Task<CoffeeBlend?> GetBlendByIdAsync(int blendId);
}
