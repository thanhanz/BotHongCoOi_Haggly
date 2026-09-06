using Dapper;
using Haggly.Domain.Modules.Markets;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Queries.Markets;
using Haggly.IntegrationTests.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Queries.Markets;

public sealed class DapperMarketQueryTests
{

  private readonly DapperDbContext _dbContext;
  private readonly DapperMarketQuery _sut;


  public DapperMarketQueryTests()
  {
    _dbContext = new DapperDbContext(IntegrationTestDatabase.CreateConfiguration());

    _sut = new DapperMarketQuery(_dbContext);
  }

  [Fact]
  public async Task GetAllAsync_WhenMarketsIncludeInactiveAndDeleted_ReturnsOnlyActiveNonDeletedMarkets()
  {
    var activeId = System.Guid.NewGuid();
    var inactiveId = System.Guid.NewGuid();
    var deletedId = System.Guid.NewGuid();

    var activeCode = $"active-{Guid.NewGuid():N}";
    var inactiveCode = $"inactive-{Guid.NewGuid():N}";
    var deletedCode = $"deleted-{Guid.NewGuid():N}";

    await SeedMarketAsync(
            activeId,
            activeCode,
            "Active Market",
            "1 Main St",
            MarketStatus.ACTIVE,
            DateTime.UtcNow,
            null);

    await SeedMarketAsync(
        inactiveId,
        inactiveCode,
        "Inactive Market",
        "12 Main St",
        MarketStatus.INACTIVE,
        DateTime.UtcNow,
        null);

    await SeedMarketAsync(
        deletedId,
        deletedCode,
        "Deleted Market",
        "123 Main St",
        MarketStatus.SUSPENDED,
        DateTime.UtcNow,
        DateTime.UtcNow);

    // Act
    var result = await _sut.GetAllAsync(CancellationToken.None);

    // Assert
    Assert.Contains(result, x => x.Id == activeId);
    Assert.DoesNotContain(result, x => x.Id == inactiveId);
    Assert.DoesNotContain(result, x => x.Id == deletedId);

  }

  [Fact]
  public async Task GetByIdAsync_WhenMarketExists_ReturnsMarket()
  {
    
    var activeId = System.Guid.NewGuid();
    var activeCode = $"active-{Guid.NewGuid():N}";

    await SeedMarketAsync(
      activeId,
      activeCode,
      "Active Market",
      "1 Main St",
      MarketStatus.ACTIVE,
      DateTime.UtcNow,
      null);
    
    var result = await _sut.GetByIdAsync(activeId, CancellationToken.None);
    
    Assert.NotNull(result);
    Assert.Equal(result.Id, activeId);
  }

  private async Task SeedMarketAsync(
    Guid id,
    string code,
    string name,
    string address,
    MarketStatus status,
    DateTime? createdAt,
    DateTime? deletedAt)
  {
    const string sql =
        """
        INSERT INTO markets.markets
            ("Id", "Code","Name", "Address", "Status", "CreatedAt", "DeletedAt")
        VALUES
            (@id, @Code, @Name, @Address, @Status, @CreatedAt, @DeletedAt);
        """;

    await using var connection =
        await _dbContext.OpenConnectionAsync(
            CancellationToken.None);

    await connection.ExecuteAsync(
        sql,
        new
        {
          Id = id,
          Code = code,
          Name = name,
          Address = address,
          Status = status.ToString(),
          CreatedAt = createdAt,
          DeletedAt = deletedAt
        });
  }


}
