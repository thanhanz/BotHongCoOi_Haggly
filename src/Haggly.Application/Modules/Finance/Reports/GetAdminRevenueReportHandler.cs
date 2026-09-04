using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Common.Time;
using MediatR;

namespace Haggly.Application.Modules.Finance.Reports;

public sealed class GetAdminRevenueReportHandler(
    IRevenueReportQuery revenueReports,
    IBusinessClock businessClock)
    : IRequestHandler<GetAdminRevenueReportQuery, AdminRevenueReportResponse>
{
    public Task<AdminRevenueReportResponse> Handle(
        GetAdminRevenueReportQuery query,
        CancellationToken cancellationToken)
    {
        var request = RevenueReportRequestFactory.CreateAdminRequest(
            businessClock.GetNow(),
            query.From,
            query.To,
            query.SaleChannel,
            query.MarketId,
            query.VendorId,
            query.StallId);

        return revenueReports.GetAdminReportAsync(request, cancellationToken);
    }
}
