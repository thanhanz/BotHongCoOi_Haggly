using Dapper;
using Haggly.Application.Abstractions.Markets;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Infrastructure.Persistence.Queries.Markets;

public sealed class DapperMarketQuery(DapperDbContext dbContext) : IMarketQuery
{
  private readonly DapperDbContext _dbContext = dbContext;

  public async Task<IReadOnlyCollection<Market>> GetAllAsync(CancellationToken cancellationToken)
  {
    var sql = """
      SELECT * FROM markets.markets
      WHERE "DeletedAt" IS NULL AND "Status" = @Status;

      """;

    await using var connection = await _dbContext.OpenConnectionAsync(cancellationToken);

    var command = new CommandDefinition(
      sql, 
      new { Status = MarketStatus.ACTIVE.ToString() },
      cancellationToken: cancellationToken);

    var markets = await connection.QueryAsync<Market>(command);

    return markets.AsList();
  }

  public async Task<Market?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
  {
    var sql = """
      SELECT * 
      FROM markets.markets
      WHERE "Id" = @Id AND 
            "DeletedAt" IS NULL AND 
            "Status" = @Status;

      """;

    await using var connection = await _dbContext.OpenConnectionAsync(cancellationToken);

    var command = new CommandDefinition(
      sql,
      new
      {
        Id = id,
        Status = MarketStatus.ACTIVE.ToString()
      },
      cancellationToken: cancellationToken);
   
    var market = await connection.QuerySingleOrDefaultAsync<Market>(command);

    return market;
  }
}

