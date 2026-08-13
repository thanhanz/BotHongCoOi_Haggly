using Dapper;
using Haggly.Application.Abstractions.Catalog;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Infrastructure.Persistence.Queries.Catalog;

public sealed class DapperCategoryQuery(DapperDbContext dbContext) : ICategoryQuery
{
    public async Task<IReadOnlyCollection<Category>> GetAllActiveAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT *
            FROM catalog.categories
            WHERE "DeletedAt" IS NULL
              AND "Status" = @Status
            ORDER BY "DisplayOrder", "Name";
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { Status = CatalogStatus.ACTIVE.ToString() },
            cancellationToken: cancellationToken);
        var categories = await connection.QueryAsync<Category>(command);

        return categories.AsList();
    }

    public async Task<Category?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT *
            FROM catalog.categories
            WHERE "Id" = @Id
              AND "DeletedAt" IS NULL
              AND "Status" = @Status;
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { Id = id, Status = CatalogStatus.ACTIVE.ToString() },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Category>(command);
    }
}
