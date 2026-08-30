using Haggly.Domain.Modules.Inventory;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Inventory;

public sealed class InventoryLedgerTests
{
    [Fact]
    public void AddItem_OpeningStock_CreatesCompleteOpeningLedger()
    {
        // Arrange
        var inventory = DomainInventory.Create(StallId, ActorId, OccurredAt);

        // Act
        var item = inventory.AddItem(ProductStallId, 10m, ActorId, OccurredAt);

        // Assert
        var ledger = Assert.Single(item.InventoryLedgers);
        Assert.Equal(InventoryTransactionType.OPENING, ledger.TransactionType);
        Assert.Equal(10m, ledger.QuantityDelta);
        Assert.Equal(0m, ledger.QuantityBefore);
        Assert.Equal(10m, ledger.QuantityAfter);
        Assert.Equal(ActorId, ledger.PerformedBy);
        Assert.Equal(OccurredAt, ledger.OccurredAt);
        Assert.Equal(nameof(InventoryItem), ledger.ReferenceType);
    }

    [Fact]
    public void AdjustQuantity_StockChange_CreatesCompleteAdjustmentLedger()
    {
        // Arrange
        var item = DomainInventory.Create(StallId, ActorId, OccurredAt)
            .AddItem(ProductStallId, 10m, ActorId, OccurredAt);

        // Act
        var ledger = item.AdjustQuantity(-3m, ActorId, AdjustmentAt, "waste");

        // Assert
        Assert.Equal(InventoryTransactionType.ADJUSTMENT, ledger.TransactionType);
        Assert.Equal(-3m, ledger.QuantityDelta);
        Assert.Equal(10m, ledger.QuantityBefore);
        Assert.Equal(7m, ledger.QuantityAfter);
        Assert.Equal(ActorId, ledger.PerformedBy);
        Assert.Equal("waste", ledger.Reason);
        Assert.Equal(AdjustmentAt, ledger.OccurredAt);
    }

    [Fact]
    public void ConsumeReservedOnlineSale_PaidQuantity_CreatesCompletePaymentLedger()
    {
        // Arrange
        var item = DomainInventory.Create(StallId, ActorId, OccurredAt)
            .AddItem(ProductStallId, 10m, ActorId, OccurredAt);
        item.Reserve(4m, OccurredAt);

        // Act
        var ledger = item.ConsumeReservedOnlineSale(3m, PaymentId, ConsumedAt);

        // Assert
        Assert.Equal(InventoryTransactionType.ONLINE_SALE, ledger.TransactionType);
        Assert.Equal(-3m, ledger.QuantityDelta);
        Assert.Equal(10m, ledger.QuantityBefore);
        Assert.Equal(7m, ledger.QuantityAfter);
        Assert.Equal(PaymentId, ledger.ReferenceId);
        Assert.Equal("PAYMENT_TRANSACTION", ledger.ReferenceType);
        Assert.Null(ledger.PerformedBy);
        Assert.Equal(ConsumedAt, ledger.OccurredAt);
    }

    private static readonly Guid StallId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductStallId = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private static readonly Guid ActorId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid PaymentId = Guid.Parse("40000000-0000-0000-0000-000000000004");
    private static readonly DateTimeOffset OccurredAt = new(2026, 8, 17, 2, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AdjustmentAt = new(2026, 8, 17, 2, 32, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ConsumedAt = new(2026, 8, 17, 2, 35, 0, TimeSpan.Zero);
}
