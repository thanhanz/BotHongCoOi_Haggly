using Dapper;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence;

public sealed class InventoryPersistenceBoundaryTests
{
    [Fact]
    public async Task InventorySession_WhenBusinessDateIsDuplicated_RejectsSecondRow()
    {
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        await using var dbContext = CreateDbContext();
        var businessDate = new DateOnly(2026, 8, 15);

        dbContext.InventorySessions.Add(InventorySession.Open(
            scenario.StallId,
            businessDate,
            DateTimeOffset.UtcNow,
            scenario.OwnerId,
            null));
        await dbContext.SaveChangesAsync();

        dbContext.InventorySessions.Add(InventorySession.Open(
            scenario.StallId,
            businessDate,
            DateTimeOffset.UtcNow,
            scenario.OwnerId,
            null));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());

        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            (exception.InnerException as PostgresException)?.SqlState);
    }

    [Fact]
    public async Task InventoryListing_WhenCheckConstraintIsViolated_RejectsNegativeQuantity()
    {
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        await using var connection = new NpgsqlConnection(IntegrationTestDatabase.ConnectionString);
        await connection.OpenAsync();
        var sessionId = Guid.NewGuid();
        await connection.ExecuteAsync(
            """
            INSERT INTO inventory.inventory_sessions
                ("Id", "StallId", "BusinessDate", "OpenedAt", "OpenedBy", "Status", "CreatedAt")
            VALUES
                (@SessionId, @StallId, @BusinessDate, @OpenedAt, @OwnerId, 'OPEN', @OpenedAt);
            """,
            new
            {
                SessionId = sessionId,
                scenario.StallId,
                BusinessDate = new DateOnly(2026, 8, 16).ToDateTime(TimeOnly.MinValue),
                OpenedAt = DateTimeOffset.UtcNow,
                scenario.OwnerId
            });

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                """
                INSERT INTO inventory.daily_product_listings
                    ("Id", "InventorySessionId", "ProductStallId", "ProductNameSnapshot",
                     "SellingUnitSnapshot", "PublicUnitPrice", "OpeningQuantity", "CurrentQuantity",
                     "ReservedQuantity", "AvailableQuantity", "Status", "Version", "CreatedAt")
                VALUES
                    (@ListingId, @SessionId, @ProductStallId, 'Invalid', 'KG', 10.00,
                     -1.000, 0.000, 0.000, 0.000, 'AVAILABLE', 0, @CreatedAt);
                """,
                new
                {
                    ListingId = Guid.NewGuid(),
                    SessionId = sessionId,
                    scenario.ProductStallId,
                    CreatedAt = DateTimeOffset.UtcNow
                }));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    [Fact]
    public async Task DailyProductListing_WhenTwoContextsUpdateSameVersion_RejectsStaleSave()
    {
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        var session = InventorySession.Open(
            scenario.StallId,
            new DateOnly(2026, 8, 17),
            DateTimeOffset.UtcNow,
            scenario.OwnerId,
            null);
        var listing = DailyProductListing.Open(
            session.Id,
            scenario.ProductStallId,
            "Integration Tomato",
            ProductUnit.KG,
            45m,
            10m,
            scenario.OwnerId,
            session.OpenedAt);
        session.DailyProductListings.Add(listing);

        await using (var seedContext = CreateDbContext())
        {
            seedContext.InventorySessions.Add(session);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = CreateDbContext();
        await using var secondContext = CreateDbContext();
        var first = await firstContext.DailyProductListings
            .Include(value => value.InventoryLedgers)
            .SingleAsync(value => value.Id == listing.Id);
        var second = await secondContext.DailyProductListings
            .Include(value => value.InventoryLedgers)
            .SingleAsync(value => value.Id == listing.Id);

        Assert.Equal(0L, firstContext.Entry(first).Property(value => value.Version).OriginalValue);
        Assert.Equal(0L, secondContext.Entry(second).Property(value => value.Version).OriginalValue);
        var storedVersion = await firstContext.Database.SqlQuery<long>(
                $"SELECT \"Version\" AS \"Value\" FROM inventory.daily_product_listings WHERE \"Id\" = {listing.Id}")
            .SingleAsync();
        Assert.Equal(0L, storedVersion);

        first.AdjustQuantity(1m, scenario.OwnerId, DateTimeOffset.UtcNow, "First update");
        await firstContext.SaveChangesAsync();

        second.AdjustQuantity(1m, scenario.OwnerId, DateTimeOffset.UtcNow, "Stale update");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task InventoryQuantityColumns_WhenModelIsMigrated_UseThreeDecimalPlaces()
    {
        await using var connection = new NpgsqlConnection(IntegrationTestDatabase.ConnectionString);
        await connection.OpenAsync();

        var scale = await connection.ExecuteScalarAsync<short>(
            """
            SELECT numeric_scale
            FROM information_schema.columns
            WHERE table_schema = 'inventory'
              AND table_name = 'daily_product_listings'
              AND column_name = 'OpeningQuantity';
            """);

        Assert.Equal((short)3, scale);
    }

    private static HagglyDbContext CreateDbContext()
        => new(
            new DbContextOptionsBuilder<HagglyDbContext>()
                .UseNpgsql(IntegrationTestDatabase.ConnectionString)
                .Options);
}
