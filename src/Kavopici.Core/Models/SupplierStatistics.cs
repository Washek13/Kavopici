namespace Kavopici.Models;

public record SupplierStatistics(
    int SupplierId,
    string SupplierName,
    int TotalSessionCount,
    int Last30DaysSessionCount
);
