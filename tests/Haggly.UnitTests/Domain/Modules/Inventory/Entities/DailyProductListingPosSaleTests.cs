using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Inventory.Entities;

public sealed class DailyProductListingPosSaleTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 15, 3, 30, 0, TimeSpan.Zero);

    [Fact]
    public void RecordPosSale_WhenQuantityIsAvailable_DecrementsStockAndCreatesSaleLedger()
    {
        var actorId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var listing = DailyProductListing.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tomato",
            ProductUnit.KG,
            45_000m,
            10m,
            actorId,
            OccurredAt);

        var ledger = listing.RecordPosSale(2.5m, saleId, actorId, OccurredAt);

        Assert.Equal(7.5m, listing.CurrentQuantity);
        Assert.Equal(7.5m, listing.AvailableQuantity);
        Assert.Equal(0m, listing.ReservedQuantity);
        Assert.Equal(1L, listing.Version);
        Assert.Equal(InventoryTransactionType.POS_SALE, ledger.TransactionType);
        Assert.Equal(-2.5m, ledger.QuantityDelta);
        Assert.Equal(10m, ledger.QuantityBefore);
        Assert.Equal(7.5m, ledger.QuantityAfter);
        Assert.Equal("POS_SALE", ledger.ReferenceType);
        Assert.Equal(saleId, ledger.ReferenceId);
    }

    [Fact]
    public void RecordPosSale_WhenQuantityExceedsAvailable_ThrowsInvalidOperationException()
    {
        var listing = DailyProductListing.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tomato",
            ProductUnit.KG,
            45_000m,
            10m,
            Guid.NewGuid(),
            OccurredAt);
        listing.UpdateReservedQuantity(4m);

        Assert.Throws<InvalidOperationException>(() => listing.RecordPosSale(
            7m,
            Guid.NewGuid(),
            Guid.NewGuid(),
            OccurredAt));
    }
}
