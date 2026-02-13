namespace Kavopici.Models;

public class TastingNote
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<RatingTastingNote> RatingTastingNotes { get; set; } = new List<RatingTastingNote>();
}
