namespace Kavopici.Models;

public class Rating
{
    public int Id { get; set; }
    public int BlendId { get; set; }
    public int UserId { get; set; }
    public int SessionId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CoffeeBlend Blend { get; set; } = null!;
    public User User { get; set; } = null!;
    public TastingSession Session { get; set; } = null!;
    public ICollection<RatingTastingNote> RatingTastingNotes { get; set; } = new List<RatingTastingNote>();
}
