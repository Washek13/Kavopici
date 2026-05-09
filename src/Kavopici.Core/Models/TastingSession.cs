namespace Kavopici.Models;

public class TastingSession
{
    public int Id { get; set; }
    public int BlendId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Comment { get; set; }
    public int? CleanupPersonId { get; set; }
    public bool? CleanupCompleted { get; set; }
    public decimal DoseMultiplier { get; set; } = 1.0m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CoffeeBlend Blend { get; set; } = null!;
    public User? CleanupPerson { get; set; }
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
