using Haggly.Application.Modules.Finance.Reports;

namespace Haggly.Application.Abstractions.Finance;

public interface IRevenueReportQuery
{
    Task<VendorRevenueReportResponse> GetVendorReportAsync(
        Guid vendorId,
        VendorRevenueReportRequest request,
        CancellationToken cancellationToken);

    Task<AdminRevenueReportResponse> GetAdminReportAsync(
        AdminRevenueReportRequest request,
        CancellationToken cancellationToken);
}
