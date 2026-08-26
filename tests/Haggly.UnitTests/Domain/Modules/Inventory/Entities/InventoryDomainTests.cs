using Haggly.Domain.Modules.Inventory;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Domain.Modules.Inventory.Entities;

public sealed class InventoryDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 2, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidStall_CreatesContinuousInventory()
    {
        var stallId = Guid.NewGuid();
        var inventory = DomainInventory.Create(stallId, Guid.NewGuid(), Now);
        Assert.Equal(stallId, inventory.StallId);
        Assert.Empty(inventory.Items);
    }

    [Fact]
    public void AddItem_ValidQuantity_InitializesStockAndOpeningLedger()
    {
        var inventory = CreateInventory();
        var item = inventory.AddItem(Guid.NewGuid(), 10.5m, Guid.NewGuid(), Now);
        Assert.Equal(10.5m, item.CurrentQuantity);
        Assert.Equal(10.5m, item.AvailableQuantity);
        Assert.Equal(InventoryTransactionType.OPENING, Assert.Single(item.InventoryLedgers).TransactionType);
    }

    [Fact]
    public void AddItem_DuplicateProductStall_ThrowsInvalidOperationException()
    {
        var inventory = CreateInventory();
        var productStallId = Guid.NewGuid();
        inventory.AddItem(productStallId, 1m, Guid.NewGuid(), Now);
        Assert.Throws<InvalidOperationException>(() =>
            inventory.AddItem(productStallId, 1m, Guid.NewGuid(), Now));
    }

    [Fact]
    public void Reserve_QuantityAvailable_IncreasesReservedQuantity()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.Reserve(4m, Now);
        Assert.Equal(4m, item.ReservedQuantity);
        Assert.Equal(6m, item.AvailableQuantity);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void Reserve_QuantityExceedsAvailable_ThrowsInvalidOperationException()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.Reserve(7m, Now);

        Assert.Throws<InvalidOperationException>(() => item.Reserve(4m, Now));
    }

    [Fact]
    public void ReleaseReserved_QuantityReserved_DecreasesReservedQuantity()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.Reserve(4m, Now);

        item.ReleaseReserved(4m, Now.AddMinutes(1));

        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(0m, item.ReservedQuantity);
        Assert.Equal(10m, item.AvailableQuantity);
    }

    [Fact]
    public void ReleaseReserved_QuantityExceedsReservation_ThrowsInvalidOperationException()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.Reserve(2m, Now);

        Assert.Throws<InvalidOperationException>(() =>
            item.ReleaseReserved(3m, Now.AddMinutes(1)));
    }

    [Fact]
    public void AdjustQuantity_ResultBelowReserved_ThrowsInvalidOperationException()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.Reserve(6m, Now);
        Assert.Throws<InvalidOperationException>(() =>
            item.AdjustQuantity(-5m, Guid.NewGuid(), Now, "Spoilage"));
    }

    [Fact]
    public void RecordSale_QuantityAvailable_DecrementsStockAndCreatesLedger()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        var ledger = item.RecordSaleDirectly(3m, Guid.NewGuid(), Guid.NewGuid(), Now);
        Assert.Equal(7m, item.CurrentQuantity);
        Assert.Equal(-3m, ledger.QuantityDelta);
        Assert.Equal(InventoryTransactionType.POS_SALE, ledger.TransactionType);
    }

    [Fact]
    public void RecordSale_QuantityExceedsAvailable_ThrowsInvalidOperationException()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.Reserve(8m, Now);
        Assert.Throws<InvalidOperationException>(() =>
            item.RecordSaleDirectly(3m, Guid.NewGuid(), Guid.NewGuid(), Now));
    }

    [Fact]
    public void ConsumeReservedOnlineSale_QuantityReserved_DecrementsStockAndReservationAndCreatesPaymentLedger()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        var paymentTransactionId = Guid.NewGuid();
        item.Reserve(3m, Now);

        var ledger = item.ConsumeReservedOnlineSale(3m, paymentTransactionId, Now);

        Assert.Equal(7m, item.CurrentQuantity);
        Assert.Equal(0m, item.ReservedQuantity);
        Assert.Equal(7m, item.AvailableQuantity);
        Assert.Equal(InventoryTransactionType.ONLINE_SALE, ledger.TransactionType);
        Assert.Equal("PAYMENT_TRANSACTION", ledger.ReferenceType);
        Assert.Equal(paymentTransactionId, ledger.ReferenceId);
        Assert.Equal(TimeSpan.Zero, ledger.OccurredAt.Offset);
    }

    [Fact]
    public void ConsumeReservedOnlineSale_QuantityExceedsReservation_ThrowsInvalidOperationException()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.Reserve(2m, Now);

        Assert.Throws<InvalidOperationException>(() =>
            item.ConsumeReservedOnlineSale(3m, Guid.NewGuid(), Now));
    }

    private static DomainInventory CreateInventory()
        => DomainInventory.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
}
