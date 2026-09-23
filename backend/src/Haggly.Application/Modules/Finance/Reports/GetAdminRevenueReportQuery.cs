using MediatR;

namespace Haggly.Application.Modules.Finance.Reports;

public sealed record GetAdminRevenueReportQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    SaleChannel? SaleChannel,
    Guid? MarketId,
    Guid? VendorId,
    Guid? StallId) : IRequest<AdminRevenueReportResponse>;
