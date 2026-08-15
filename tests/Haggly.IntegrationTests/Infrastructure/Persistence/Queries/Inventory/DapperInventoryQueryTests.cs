using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Queries.Inventory;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Queries.Inventory;

public sealed class DapperInventoryQueryTests
{
    [Fact]
    public async Task GetCurrentAndPreviousSession_WhenMultipleDatesExist_ReturnsStallScopedSessionsWithListings()
    {
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        var previousDate = new DateOnly(2026, 8, 18);
        var currentDate = new DateOnly(2026, 8, 19);
        var previous = await SeedSessionAsync(scenario, previousDate, 5m, "Previous");
        var current = await SeedSessionAsync(scenario, currentDate, 10m, "Current");
        var sut = new DapperInventoryQuery(
            new DapperDbContext(IntegrationTestDatabase.CreateConfiguration()));

        var currentResult = await sut.GetCurrentSessionAsync(
            scenario.StallId,
            currentDate,
            CancellationToken.None);
        var previousResult = await sut.GetPreviousSessionAsync(
            scenario.StallId,
            currentDate,
            CancellationToken.None);

        Assert.NotNull(currentResult);
        Assert.Equal(current.Id, currentResult.Id);
        Assert.Equal("Current", Assert.Single(currentResult.DailyProductListings).ProductNameSnapshot);
        Assert.NotNull(previousResult);
        Assert.Equal(previous.Id, previousResult.Id);
        Assert.Equal("Previous", Assert.Single(previousResult.DailyProductListings).ProductNameSnapshot);
    }

    [Fact]
    public async Task GetLedger_WhenFiltersAndPagingAreApplied_ReturnsStableDescendingHistory()
    {
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        var date = new DateOnly(2026, 8, 20);
        var session = await SeedSessionAsync(scenario, date, 10m, "Ledger");
        var listingId = session.DailyProductListings.Single().Id;

        await using (var dbContext = CreateDbContext())
        {
            var listing = await dbContext.DailyProductListings
                .Include(value => value.InventoryLedgers)
                .SingleAsync(value => value.Id == listingId);
            listing.AdjustQuantity(1m, scenario.OwnerId, session.OpenedAt.AddMinutes(1), "Restock");
            listing.ChangePrice(50m, scenario.OwnerId, session.OpenedAt.AddMinutes(2));
            await dbContext.SaveChangesAsync();
        }

        var sut = new DapperInventoryQuery(
            new DapperDbContext(IntegrationTestDatabase.CreateConfiguration()));
        var result = await sut.GetLedgerAsync(
            new InventoryLedgerListFilter(
                scenario.StallId,
                date,
                listingId,
                null,
                1,
                2),
            CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        var items = result.Items.ToArray();
        Assert.Equal(InventoryTransactionType.PRICE_CHANGE, items[0].TransactionType);
        Assert.Equal(InventoryTransactionType.ADJUSTMENT, items[1].TransactionType);
        Assert.True(items[0].OccurredAt >= items[1].OccurredAt);

        var adjustmentPage = await sut.GetLedgerAsync(
            new InventoryLedgerListFilter(
                scenario.StallId,
                date,
                listingId,
                InventoryTransactionType.ADJUSTMENT,
                1,
                20),
            CancellationToken.None);

        Assert.Equal(1, adjustmentPage.TotalCount);
        Assert.Equal(InventoryTransactionType.ADJUSTMENT,
            Assert.Single(adjustmentPage.Items).TransactionType);
    }

    private static async Task<InventorySession> SeedSessionAsync(
        InventoryIntegrationScenario scenario,
        DateOnly date,
        decimal quantity,
        string productName)
    {
        var session = InventorySession.Open(
            scenario.StallId,
            date,
            new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            scenario.OwnerId,
            productName);
        var listing = DailyProductListing.Open(
            session.Id,
            scenario.ProductStallId,
            productName,
            ProductUnit.KG,
            45m,
            quantity,
            scenario.OwnerId,
            session.OpenedAt);
        session.DailyProductListings.Add(listing);

        await using var dbContext = CreateDbContext();
        dbContext.InventorySessions.Add(session);
        await dbContext.SaveChangesAsync();
        return session;
    }

    private static HagglyDbContext CreateDbContext()
        => new(
            new DbContextOptionsBuilder<HagglyDbContext>()
                .UseNpgsql(IntegrationTestDatabase.ConnectionString)
                .Options);
}
