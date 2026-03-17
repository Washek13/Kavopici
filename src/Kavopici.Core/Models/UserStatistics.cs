namespace Kavopici.Models;

public record UserStatistics(
    int UserId,
    string UserName,
    int VoteCount,
    double AverageGiven,
    double ParticipationRate,
    string? FavoriteTastingNote,
    int SuppliedBlendsCount,
    decimal? SuppliedAvgPricePerStar,
    double? VotingConsistency   // population std dev; null if < 2 votes
);
