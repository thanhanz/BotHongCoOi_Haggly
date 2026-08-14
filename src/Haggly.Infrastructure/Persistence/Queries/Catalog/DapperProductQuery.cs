using Dapper;
using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Queries.Products;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Infrastructure.Persistence.Queries.Catalog;

public sealed class DapperProductQuery(DapperDbContext dbContext) : IProductQuery
{
    public async Task<PagedResult<Product>> GetPageAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM catalog.products
            WHERE "DeletedAt" IS NULL
              AND "Status" = @Status
              AND (@CategoryId IS NULL OR "CategoryId" = @CategoryId);

            SELECT *
            FROM catalog.products
            WHERE "DeletedAt" IS NULL
              AND "Status" = @Status
              AND (@CategoryId IS NULL OR "CategoryId" = @CategoryId)
            ORDER BY "Name", "Id"
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                filter.CategoryId,
                Status = CatalogStatus.ACTIVE.ToString(),
                Offset = (filter.Page - 1) * filter.PageSize,
                filter.PageSize
            },
            cancellationToken: cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);
        var totalCount = checked((int)await results.ReadSingleAsync<long>());
        var products = (await results.ReadAsync<Product>()).AsList();

        return new PagedResult<Product>(products, filter.Page, filter.PageSize, totalCount);
    }

    public async Task<Product?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT *
            FROM catalog.products
            WHERE "Id" = @Id
              AND "DeletedAt" IS NULL
              AND "Status" = @Status;
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { Id = id, Status = CatalogStatus.ACTIVE.ToString() },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Product>(command);
    }
}
