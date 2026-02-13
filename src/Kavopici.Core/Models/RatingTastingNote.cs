namespace Kavopici.Models;

public class RatingTastingNote
{
    public int RatingId { get; set; }
    public int TastingNoteId { get; set; }

    public Rating Rating { get; set; } = null!;
    public TastingNote TastingNote { get; set; } = null!;
}
