using Dapper;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Infrastructure.Persistence.Queries.Sales;

public sealed class DapperPosSaleQuery(DapperDbContext db) : IPosSaleQuery
{
    public async Task<PagedResult<PosSale>> GetPageAsync(
        Guid stallId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM sales.pos_sales
            WHERE "StallId" = @StallId;

            SELECT "Id", "StallId", "SaleNo", "ClientRequestId", "Status", "TotalAmount",
                   "CompletedBy", "CompletedAt", "PaymentMethod", "PaymentStatus", "AmountPaid",
                   "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
            FROM sales.pos_sales
            WHERE "StallId" = @StallId
            ORDER BY "CompletedAt" DESC, "Id" DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await db.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(
                sql,
                new
                {
                    StallId = stallId,
                    Offset = checked((page - 1) * pageSize),
                    PageSize = pageSize
                },
                cancellationToken: cancellationToken));

        var total = checked((int)await results.ReadSingleAsync<long>());
        var saleRows = (await results.ReadAsync<PosSale>()).AsList();
        return new(saleRows, page, pageSize, total);
    }
}
