using Dapper;
using Haggly.Application.Abstractions.Catalog;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Infrastructure.Persistence.Queries.Catalog;

public sealed class DapperProductQuery(DapperDbContext dbContext) : IProductQuery
{
    public async Task<IReadOnlyCollection<Product>> GetAllActiveAsync(
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT *
            FROM catalog.products
            WHERE "DeletedAt" IS NULL
              AND "Status" = @Status
              AND (@CategoryId IS NULL OR "CategoryId" = @CategoryId)
            ORDER BY "Name";
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { CategoryId = categoryId, Status = CatalogStatus.ACTIVE.ToString() },
            cancellationToken: cancellationToken);
        var products = await connection.QueryAsync<Product>(command);

        return products.AsList();
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
