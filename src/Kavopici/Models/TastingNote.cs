namespace Kavopici.Models;

public class TastingNote
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<RatingTastingNote> RatingTastingNotes { get; set; } = new List<RatingTastingNote>();
}

public class RatingTastingNote
{
    public int RatingId { get; set; }
    public int TastingNoteId { get; set; }

    public Rating Rating { get; set; } = null!;
    public TastingNote TastingNote { get; set; } = null!;
}
