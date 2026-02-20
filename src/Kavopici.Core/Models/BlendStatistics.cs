using Kavopici.Models.Enums;

namespace Kavopici.Models;

public record BlendStatistics(
    int BlendId,
    string BlendName,
    string Roaster,
    string? Origin,
    RoastLevel RoastLevel,
    string SupplierName,
    double AverageRating,
    int RatingCount,
    int[] Distribution, // index 0-4 for stars 1-5
    decimal? PricePerKg,
    double? ControversyLevel, // population variance of stars, null when RatingCount < 2
    decimal? PricePerformance // PricePerKg / AverageRating, null when either is missing/zero
);
