using Dapper;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Queries.Inventory;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Queries.Inventory;

public sealed class DapperInventoryQueryTests
{
    [Fact]
    public async Task GetInventoryAsync_ExistingInventory_ReturnsItems()
    {
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        var itemId = Guid.NewGuid();
        var db = new DapperDbContext(IntegrationTestDatabase.CreateConfiguration());
        await using (var connection = await db.OpenConnectionAsync(CancellationToken.None))
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO inventory.inventory_items
                    ("Id", "InventoryId", "ProductStallId", "CurrentQuantity", "ReservedQuantity", "Version", "CreatedAt")
                VALUES (@ItemId, @InventoryId, @ProductStallId, 8, 2, 0, @Now);
                """, new { ItemId = itemId, scenario.InventoryId, scenario.ProductStallId, Now = DateTimeOffset.UtcNow });
        }

        var result = await new DapperInventoryQuery(db)
            .GetInventoryAsync(scenario.StallId, CancellationToken.None);

        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal(8m, item.CurrentQuantity);
        Assert.Equal(6m, item.AvailableQuantity);
    }
}
