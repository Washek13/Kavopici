namespace Kavopici.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<CoffeeBlend> SuppliedBlends { get; set; } = new List<CoffeeBlend>();
}
