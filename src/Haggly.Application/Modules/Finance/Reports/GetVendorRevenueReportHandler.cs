using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Finance.Exceptions;
using MediatR;

namespace Haggly.Application.Modules.Finance.Reports;

public sealed class GetVendorRevenueReportHandler(
    IRevenueReportQuery revenueReports,
    IBusinessClock businessClock)
    : IRequestHandler<GetVendorRevenueReportQuery, VendorRevenueReportResponse>
{
    public async Task<VendorRevenueReportResponse> Handle(
        GetVendorRevenueReportQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ActorUserId == Guid.Empty)
        {
            throw new RevenueReportValidationException("A valid vendor ID is required.");
        }

        var request = RevenueReportRequestFactory.CreateVendorRequest(
            businessClock.GetNow(),
            query.From,
            query.To,
            query.SaleChannel,
            query.StallId);

        if (request.StallId is Guid stallId
            && !await revenueReports.IsStallOwnedByVendorAsync(
                stallId,
                query.ActorUserId,
                cancellationToken))
        {
            throw new RevenueReportNotFoundException("The stall was not found.");
        }

        return await revenueReports.GetVendorReportAsync(
            query.ActorUserId,
            request,
            cancellationToken);
    }
}
