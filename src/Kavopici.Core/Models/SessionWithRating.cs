namespace Kavopici.Models;

public record SessionWithRating(
    TastingSession Session,
    Rating? UserRating,
    bool HasRated
);
