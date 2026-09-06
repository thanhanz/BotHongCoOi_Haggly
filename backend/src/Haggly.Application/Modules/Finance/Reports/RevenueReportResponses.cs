namespace Haggly.Application.Modules.Finance.Reports;

public sealed record RevenueStallSummaryResponse(
    Guid StallId,
    string StallName,
    int TotalSales,
    decimal NetRevenue);

public sealed record VendorRevenueReportResponse(
    int TotalSales,
    decimal NetRevenue,
    IReadOnlyCollection<RevenueStallSummaryResponse> Stalls);

public sealed record RevenueVendorSummaryResponse(
    Guid VendorId,
    string VendorName,
    int TotalSales,
    decimal NetRevenue,
    IReadOnlyCollection<RevenueStallSummaryResponse> Stalls);

public sealed record AdminRevenueReportResponse(
    int TotalSales,
    decimal NetRevenue,
    IReadOnlyCollection<RevenueVendorSummaryResponse> Vendors);
