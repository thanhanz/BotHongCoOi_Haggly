using Dapper;
using Haggly.Application.Abstractions.Markets;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Infrastructure.Persistence.Queries.Markets;

public sealed class DapperStallQuery(DapperDbContext dbContext) : IStallQuery
{
    private readonly DapperDbContext _dbContext = dbContext;

    public async Task<IReadOnlyCollection<Stall>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT *
            FROM markets.stalls
            WHERE "DeletedAt" IS NULL;
            """;

        await using var connection =
            await _dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var stalls = await connection.QueryAsync<Stall>(command);

        return stalls.AsList();
    }

    public async Task<Stall?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT *
            FROM markets.stalls
            WHERE "Id" = @Id
              AND "DeletedAt" IS NULL;
            """;

        await using var connection =
            await _dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { Id = id },
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Stall>(command);
    }
}
