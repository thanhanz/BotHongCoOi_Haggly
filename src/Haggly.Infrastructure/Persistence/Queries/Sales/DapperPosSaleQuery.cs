using Dapper;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Infrastructure.Persistence.Queries.Sales;

public sealed class DapperPosSaleQuery(DapperDbContext db) : IPosSaleQuery
{
    public async Task<PosSale?> GetByIdWithItemsAsync(
        Guid stallId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id", "StallId", "SaleNo", "ClientRequestId", "Status", "TotalAmount",
                   "CompletedBy", "CompletedAt", "PaymentMethod", "PaymentStatus", "AmountPaid",
                   "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
            FROM sales.pos_sales
            WHERE "Id" = @SaleId AND "StallId" = @StallId;

            SELECT "Id", "PosSaleId", "InventoryItemId", "ProductNameSnapshot", "SellingUnitSnapshot",
                   "UnitPrice", "Quantity", "LineTotal", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
            FROM sales.pos_sale_items
            WHERE "PosSaleId" = @SaleId
            ORDER BY "Id";
            """;

        await using var connection = await db.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(
                sql,
                new { StallId = stallId, SaleId = saleId },
                cancellationToken: cancellationToken));

        var sale = await results.ReadSingleOrDefaultAsync<PosSale>();
        if (sale is null)
        {
            return null;
        }

        foreach (var item in await results.ReadAsync<PosSaleItem>())
        {
            sale.Items.Add(item);
        }

        return sale;
    }

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
