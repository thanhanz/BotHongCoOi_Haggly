using Haggly.Application.Modules.Finance.Reports;

namespace Haggly.Application.Abstractions.Finance;

public interface IRevenueReportQuery
{
    Task<bool> IsStallOwnedByVendorAsync(
        Guid stallId,
        Guid vendorId,
        CancellationToken cancellationToken);

    Task<VendorRevenueReportResponse> GetVendorReportAsync(
        Guid vendorId,
        VendorRevenueReportRequest request,
        CancellationToken cancellationToken);

    Task<AdminRevenueReportResponse> GetAdminReportAsync(
        AdminRevenueReportRequest request,
        CancellationToken cancellationToken);
}
