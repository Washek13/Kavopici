namespace Kavopici.Models;

public record SupplierStatistics(
    int SupplierId,
    string SupplierName,
    decimal TotalDoses,
    decimal Last30DaysDoses
);
