using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Inventory.Entities;

public sealed class InventoryDomainTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 15, 1, 30, 0, TimeSpan.Zero);

    [Fact]
    public void CreateOpeningListing_WhenValuesAreValid_InitializesQuantitiesAndCreatesOpeningLedger()
    {
        var sessionId = Guid.NewGuid();
        var productStallId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var listing = DailyProductListing.CreateOpening(
            sessionId,
            productStallId,
            productNameSnapshot: "Tomato",
            sellingUnitSnapshot: ProductUnit.KG,
            publicUnitPrice: 45_000m,
            openingQuantity: 25.5m,
            actorId,
            OccurredAt);

        Assert.Equal(25.5m, listing.OpeningQuantity);
        Assert.Equal(25.5m, listing.CurrentQuantity);
        Assert.Equal(0m, listing.ReservedQuantity);
        Assert.Equal(25.5m, listing.AvailableQuantity);
        Assert.Equal(DailyListingStatus.AVAILABLE, listing.Status);
        Assert.Equal(0L, listing.Version);

        var ledger = Assert.Single(listing.InventoryLedgers);
        Assert.Equal(InventoryTransactionType.OPENING, ledger.TransactionType);
        Assert.Equal(25.5m, ledger.QuantityDelta);
        Assert.Equal(0m, ledger.QuantityBefore);
        Assert.Equal(25.5m, ledger.QuantityAfter);
        Assert.Equal(actorId, ledger.PerformedBy);
        Assert.Equal(OccurredAt, ledger.OccurredAt);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(1, -1)]
    public void CreateOpeningListing_WhenQuantityOrPriceIsNegative_ThrowsArgumentOutOfRangeException(
        decimal openingQuantity,
        decimal publicUnitPrice)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyProductListing.CreateOpening(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tomato",
            ProductUnit.KG,
            publicUnitPrice,
            openingQuantity,
            Guid.NewGuid(),
            OccurredAt));
    }

    [Fact]
    public void RefreshAvailableQuantity_WhenListingIsHidden_PreservesHiddenStatus()
    {
        var listing = CreateListing(openingQuantity: 10m);
        listing.Status = DailyListingStatus.HIDDEN;
        listing.ReservedQuantity = 3m;

        listing.RefreshAvailableQuantity();

        Assert.Equal(7m, listing.AvailableQuantity);
        Assert.Equal(DailyListingStatus.HIDDEN, listing.Status);
    }

    [Fact]
    public void AdjustQuantity_WhenDeltaIsValid_IncrementsVersionAndCreatesAdjustmentLedger()
    {
        var actorId = Guid.NewGuid();
        var listing = CreateListing(openingQuantity: 10m);

        var ledger = listing.AdjustQuantity(
            quantityDelta: -2.5m,
            actorId: actorId,
            occurredAt: OccurredAt,
            reason: "Damaged stock");

        Assert.Equal(7.5m, listing.CurrentQuantity);
        Assert.Equal(7.5m, listing.AvailableQuantity);
        Assert.Equal(1L, listing.Version);
        Assert.Equal(InventoryTransactionType.ADJUSTMENT, ledger.TransactionType);
        Assert.Equal(-2.5m, ledger.QuantityDelta);
        Assert.Equal(10m, ledger.QuantityBefore);
        Assert.Equal(7.5m, ledger.QuantityAfter);
        Assert.Equal(actorId, ledger.PerformedBy);
        Assert.Equal("Damaged stock", ledger.Reason);
        Assert.Equal(OccurredAt, ledger.OccurredAt);
    }

    [Fact]
    public void AdjustQuantity_WhenDeltaIsZero_ThrowsArgumentOutOfRangeException()
    {
        var listing = CreateListing(openingQuantity: 10m);

        Assert.Throws<ArgumentOutOfRangeException>(() => listing.AdjustQuantity(
            quantityDelta: 0m,
            actorId: Guid.NewGuid(),
            occurredAt: OccurredAt,
            reason: "No change"));
    }

    [Theory]
    [InlineData(-11, 0)]
    [InlineData(-6, 5)]
    public void AdjustQuantity_WhenResultIsNegativeOrBelowReserved_ThrowsInvalidOperationException(
        decimal quantityDelta,
        decimal reservedQuantity)
    {
        var listing = CreateListing(openingQuantity: 10m);
        listing.ReservedQuantity = reservedQuantity;
        listing.RefreshAvailableQuantity();

        Assert.Throws<InvalidOperationException>(() => listing.AdjustQuantity(
            quantityDelta: quantityDelta,
            actorId: Guid.NewGuid(),
            occurredAt: OccurredAt,
            reason: "Invalid adjustment"));
    }

    [Fact]
    public void ChangePrice_WhenPriceChanges_IncrementsVersionAndCreatesPriceLedger()
    {
        var actorId = Guid.NewGuid();
        var listing = CreateListing(openingQuantity: 10m, publicUnitPrice: 45_000m);

        var ledger = listing.ChangePrice(50_000m, actorId, OccurredAt);

        Assert.Equal(50_000m, listing.PublicUnitPrice);
        Assert.Equal(1L, listing.Version);
        Assert.Equal(InventoryTransactionType.PRICE_CHANGE, ledger.TransactionType);
        Assert.Equal(45_000m, ledger.UnitPriceBefore);
        Assert.Equal(50_000m, ledger.UnitPriceAfter);
        Assert.Equal(actorId, ledger.PerformedBy);
        Assert.Equal(OccurredAt, ledger.OccurredAt);
    }

    [Fact]
    public void Hide_WhenCalled_PreservesQuantitiesAndMarksListingHidden()
    {
        var listing = CreateListing(openingQuantity: 10m);
        listing.ReservedQuantity = 2m;
        listing.RefreshAvailableQuantity();

        listing.Hide();

        Assert.Equal(DailyListingStatus.HIDDEN, listing.Status);
        Assert.Equal(10m, listing.CurrentQuantity);
        Assert.Equal(2m, listing.ReservedQuantity);
        Assert.Equal(8m, listing.AvailableQuantity);
    }

    [Fact]
    public void Close_WhenSessionIsOpen_RecordsClosedAuditAndChangesStatus()
    {
        var closedBy = Guid.NewGuid();
        var session = InventorySession.Open(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 15),
            OccurredAt,
            Guid.NewGuid(),
            notes: "Morning stock count");

        var closedAt = OccurredAt.AddHours(8);
        session.Close(closedBy, closedAt);

        Assert.Equal(InventorySessionStatus.CLOSED, session.Status);
        Assert.Equal(closedAt, session.ClosedAt);
        Assert.Equal(closedBy, session.ClosedBy);
    }

    [Fact]
    public void Close_WhenSessionIsAlreadyClosed_ThrowsInvalidOperationException()
    {
        var session = InventorySession.Open(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 15),
            OccurredAt,
            Guid.NewGuid(),
            notes: null);
        session.Close(Guid.NewGuid(), OccurredAt.AddHours(8));

        Assert.Throws<InvalidOperationException>(() =>
        {
            session.Close(Guid.NewGuid(), OccurredAt.AddHours(9));
        });
    }

    private static DailyProductListing CreateListing(
        decimal openingQuantity,
        decimal publicUnitPrice = 45_000m)
        => DailyProductListing.CreateOpening(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tomato",
            ProductUnit.KG,
            publicUnitPrice,
            openingQuantity,
            Guid.NewGuid(),
            OccurredAt);
}
