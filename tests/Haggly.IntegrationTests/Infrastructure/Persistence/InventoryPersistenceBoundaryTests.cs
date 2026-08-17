using Dapper;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence;

public sealed class InventoryPersistenceBoundaryTests
{
    [Fact]
    public async Task Inventory_DuplicateStall_RejectsSecondInventory()
    {
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        await using var connection = new NpgsqlConnection(IntegrationTestDatabase.ConnectionString);
        await connection.OpenAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO inventory.inventories ("Id", "StallId", "CreatedAt")
            VALUES (@Id, @StallId, @Now);
            """, new { Id = Guid.NewGuid(), scenario.StallId, Now = DateTimeOffset.UtcNow }));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
    }

    [Fact]
    public async Task InventoryItem_ReservedExceedsCurrent_RejectsRow()
    {
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        await using var connection = new NpgsqlConnection(IntegrationTestDatabase.ConnectionString);
        await connection.OpenAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO inventory.inventory_items
                ("Id", "InventoryId", "ProductStallId", "CurrentQuantity", "ReservedQuantity", "Version", "CreatedAt")
            VALUES (@Id, @InventoryId, @ProductStallId, 1, 2, 0, @Now);
            """, new { Id = Guid.NewGuid(), scenario.InventoryId, scenario.ProductStallId, Now = DateTimeOffset.UtcNow }));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    [Fact]
    public async Task InventoryItem_TwoContextsUpdateSameVersion_RejectsStaleSave()
    {
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        var itemId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(IntegrationTestDatabase.ConnectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync(
                """
                INSERT INTO inventory.inventory_items
                    ("Id", "InventoryId", "ProductStallId", "CurrentQuantity", "ReservedQuantity", "Version", "CreatedAt")
                VALUES (@ItemId, @InventoryId, @ProductStallId, 10, 0, 0, @Now);
                """, new { ItemId = itemId, scenario.InventoryId, scenario.ProductStallId, Now = DateTimeOffset.UtcNow });
        }

        await using var firstContext = CreateDbContext();
        await using var secondContext = CreateDbContext();
        var first = await firstContext.InventoryItems.Include(item => item.InventoryLedgers).SingleAsync(item => item.Id == itemId);
        var second = await secondContext.InventoryItems.Include(item => item.InventoryLedgers).SingleAsync(item => item.Id == itemId);
        first.AdjustQuantity(1m, scenario.OwnerId, DateTimeOffset.UtcNow, "First");
        await firstContext.SaveChangesAsync();
        second.AdjustQuantity(1m, scenario.OwnerId, DateTimeOffset.UtcNow, "Stale");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    private static HagglyDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql(IntegrationTestDatabase.ConnectionString).Options);
}
