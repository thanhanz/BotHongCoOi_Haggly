using MediatR;

namespace Haggly.Application.Modules.Finance.Reports;

public sealed record GetVendorRevenueReportQuery(
    Guid ActorUserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    SaleChannel? SaleChannel,
    Guid? StallId) : IRequest<VendorRevenueReportResponse>;
