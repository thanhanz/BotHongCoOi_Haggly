using Dapper;
using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Queries.ProductStalls;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Infrastructure.Persistence.Queries.Catalog;

public sealed class DapperProductStallQuery(DapperDbContext db) : IProductStallQuery
{
    public async Task<PagedResult<ProductStall>> GetProductsStallAsync(ProductStallListFilter filter, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(*) FROM catalog.product_stalls
                            WHERE "DeletedAt" IS NULL 
                              AND "StallId" = @StallId;

            SELECT * FROM catalog.product_stalls 
                     WHERE "DeletedAt" IS NULL 
                       AND "StallId" = @StallId
                     ORDER BY "DisplayName", "Id" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        await using var connection = await db.OpenConnectionAsync(ct);
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { filter.StallId, Offset = (filter.Page - 1) * filter.PageSize, filter.PageSize }, cancellationToken: ct));
        var total = checked((int)await results.ReadSingleAsync<long>());
        var items = (await results.ReadAsync<ProductStall>()).AsList();
        return new(items, filter.Page, filter.PageSize, total);
    }

    public async Task<ProductStall?> GetActiveByIdAsync(Guid stallId, Guid id, CancellationToken ct)
    {
      const string sql = """
                         SELECT * FROM catalog.product_stalls 
                                  WHERE \"Id\" = @id 
                                    AND \"StallId\" = @stallId 
                                    AND \"DeletedAt\" IS NULL;
                         """;
        await using var connection = await db.OpenConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<ProductStall>(new CommandDefinition(sql, new { id, stallId }, cancellationToken: ct));
    }
}
