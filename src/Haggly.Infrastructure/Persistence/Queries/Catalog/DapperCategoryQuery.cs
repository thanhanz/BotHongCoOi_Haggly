using Dapper;
using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Infrastructure.Persistence.Queries.Catalog;

public sealed class DapperCategoryQuery(DapperDbContext dbContext) : ICategoryQuery
{
    public async Task<PagedResult<Category>> GetPageAsync(
        CategoryListFilter filter,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM catalog.categories
            WHERE "DeletedAt" IS NULL
              AND "Status" = @Status;

            SELECT *
            FROM catalog.categories
            WHERE "DeletedAt" IS NULL
              AND "Status" = @Status
            ORDER BY "DisplayOrder", "Name", "Id"
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                Status = CatalogStatus.ACTIVE.ToString(),
                Offset = (filter.Page - 1) * filter.PageSize,
                filter.PageSize
            },
            cancellationToken: cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);
        var totalCount = checked((int)await results.ReadSingleAsync<long>());
        var categories = (await results.ReadAsync<Category>()).AsList();

        return new PagedResult<Category>(categories, filter.Page, filter.PageSize, totalCount);
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
