using Dapper;
using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Modules.Finance.Reports;

namespace Haggly.Infrastructure.Persistence.Repositories.Finance;

public sealed class DapperRevenueReportRepository(DapperDbContext dbContext)
    : IRevenueReportQuery
{
    public async Task<bool> IsStallOwnedByVendorAsync(
        Guid stallId,
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM markets.stalls
                WHERE "Id" = @StallId
                  AND "VendorId" = @VendorId
                  AND "DeletedAt" IS NULL
            );
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new { StallId = stallId, VendorId = vendorId },
            cancellationToken: cancellationToken));
    }

    public async Task<VendorRevenueReportResponse> GetVendorReportAsync(
        Guid vendorId,
        VendorRevenueReportRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH revenue_by_stall AS (
                SELECT
                    revenue."StallId",
                    COUNT(*) AS "TotalSales",
                    COALESCE(SUM(revenue."NetAmount"), 0) AS "NetRevenue"
                FROM finance.revenue_ledgers revenue
                WHERE revenue."EntryType" = 'SALE'
                  AND revenue."OccurredAt" >= @From
                  AND revenue."OccurredAt" <= @To
                  AND (
                        @SaleChannel = 'ALL'
                        OR (@SaleChannel = 'POS' AND revenue."PosSaleId" IS NOT NULL)
                        OR (@SaleChannel = 'ONLINE' AND revenue."PaymentAllocationId" IS NOT NULL)
                      )
                GROUP BY revenue."StallId"
            )
            SELECT
                stall."Id" AS "StallId",
                stall."Name" AS "StallName",
                COALESCE(revenue."TotalSales", 0) AS "TotalSales",
                COALESCE(revenue."NetRevenue", 0) AS "NetRevenue"
            FROM markets.stalls stall
            LEFT JOIN revenue_by_stall revenue ON revenue."StallId" = stall."Id"
            WHERE stall."VendorId" = @VendorId
              AND stall."DeletedAt" IS NULL
              AND (@StallId IS NULL OR stall."Id" = @StallId)
            ORDER BY stall."Name", stall."Id";
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<StallRevenueRow>(new CommandDefinition(
            sql,
            CreateParameters(request, vendorId),
            cancellationToken: cancellationToken))).AsList();

        var stalls = rows.Select(ToStallResponse).ToArray();
        return new VendorRevenueReportResponse(
            SumSales(rows),
            rows.Sum(row => row.NetRevenue),
            stalls);
    }

    public async Task<AdminRevenueReportResponse> GetAdminReportAsync(
        AdminRevenueReportRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH revenue_by_stall AS (
                SELECT
                    revenue."StallId",
                    COUNT(*) AS "TotalSales",
                    COALESCE(SUM(revenue."NetAmount"), 0) AS "NetRevenue"
                FROM finance.revenue_ledgers revenue
                WHERE revenue."EntryType" = 'SALE'
                  AND revenue."OccurredAt" >= @From
                  AND revenue."OccurredAt" <= @To
                  AND (
                        @SaleChannel = 'ALL'
                        OR (@SaleChannel = 'POS' AND revenue."PosSaleId" IS NOT NULL)
                        OR (@SaleChannel = 'ONLINE' AND revenue."PaymentAllocationId" IS NOT NULL)
                      )
                GROUP BY revenue."StallId"
            )
            SELECT
                vendor."UserId" AS "VendorId",
                vendor."BusinessName" AS "VendorName",
                stall."Id" AS "StallId",
                stall."Name" AS "StallName",
                COALESCE(revenue."TotalSales", 0) AS "TotalSales",
                COALESCE(revenue."NetRevenue", 0) AS "NetRevenue"
            FROM identity.vendor_profiles vendor
            INNER JOIN identity.users user_account ON user_account."Id" = vendor."UserId"
            INNER JOIN markets.stalls stall ON stall."VendorId" = vendor."UserId"
            LEFT JOIN revenue_by_stall revenue ON revenue."StallId" = stall."Id"
            WHERE user_account."DeletedAt" IS NULL
              AND stall."DeletedAt" IS NULL
              AND (@MarketId IS NULL OR stall."MarketId" = @MarketId)
              AND (@VendorId IS NULL OR vendor."UserId" = @VendorId)
              AND (@StallId IS NULL OR stall."Id" = @StallId)
            ORDER BY vendor."BusinessName", vendor."UserId", stall."Name", stall."Id";
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<VendorStallRevenueRow>(new CommandDefinition(
            sql,
            CreateParameters(request),
            cancellationToken: cancellationToken))).AsList();

        var vendors = rows
            .GroupBy(row => new { row.VendorId, row.VendorName })
            .Select(group => new RevenueVendorSummaryResponse(
                group.Key.VendorId,
                group.Key.VendorName,
                SumSales(group),
                group.Sum(row => row.NetRevenue),
                group.Select(ToStallResponse).ToArray()))
            .ToArray();

        return new AdminRevenueReportResponse(
            SumSales(rows),
            rows.Sum(row => row.NetRevenue),
            vendors);
    }

    private static object CreateParameters(
        VendorRevenueReportRequest request,
        Guid vendorId)
        => new
        {
            VendorId = vendorId,
            request.StallId,
            request.From,
            request.To,
            SaleChannel = request.SaleChannel.ToString()
        };

    private static object CreateParameters(AdminRevenueReportRequest request)
        => new
        {
            request.MarketId,
            request.VendorId,
            request.StallId,
            request.From,
            request.To,
            SaleChannel = request.SaleChannel.ToString()
        };

    private static RevenueStallSummaryResponse ToStallResponse(StallRevenueRow row)
        => new(row.StallId, row.StallName, checked((int)row.TotalSales), row.NetRevenue);

    private static int SumSales<T>(IEnumerable<T> rows) where T : StallRevenueRow
        => checked((int)rows.Sum(row => row.TotalSales));

    private record StallRevenueRow(
        Guid StallId,
        string StallName,
        long TotalSales,
        decimal NetRevenue);

    private sealed record VendorStallRevenueRow(
        Guid VendorId,
        string VendorName,
        Guid StallId,
        string StallName,
        long TotalSales,
        decimal NetRevenue)
        : StallRevenueRow(StallId, StallName, TotalSales, NetRevenue);
}
