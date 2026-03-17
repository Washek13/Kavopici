using Kavopici.Models.Enums;

namespace Kavopici.Models;

public class CoffeeBlend
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Roaster { get; set; } = string.Empty;
    public string? Origin { get; set; }
    public RoastLevel RoastLevel { get; set; }
    public int SupplierId { get; set; }
    public int? WeightGrams { get; set; }
    public decimal? PriceCzk { get; set; }
    public decimal? PricePerKg { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? LinkedBlendId { get; set; }

    public User Supplier { get; set; } = null!;
    public CoffeeBlend? LinkedBlend { get; set; }
    public ICollection<CoffeeBlend> LinkedBlends { get; set; } = new List<CoffeeBlend>();
    public ICollection<TastingSession> Sessions { get; set; } = new List<TastingSession>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
