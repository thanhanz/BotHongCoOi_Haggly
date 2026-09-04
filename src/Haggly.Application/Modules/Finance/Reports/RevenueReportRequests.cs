namespace Haggly.Application.Modules.Finance.Reports;

public sealed record VendorRevenueReportRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    SaleChannel SaleChannel,
    Guid? StallId);

public sealed record AdminRevenueReportRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    SaleChannel SaleChannel,
    Guid? MarketId,
    Guid? VendorId,
    Guid? StallId);
