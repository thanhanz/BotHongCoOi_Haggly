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
    public void UpdateReservedQuantity_ValidQuantity_ComputesAvailableQuantity()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.UpdateReservedQuantity(4m);
        Assert.Equal(6m, item.AvailableQuantity);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void AdjustQuantity_ResultBelowReserved_ThrowsInvalidOperationException()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.UpdateReservedQuantity(6m);
        Assert.Throws<InvalidOperationException>(() =>
            item.AdjustQuantity(-5m, Guid.NewGuid(), Now, "Spoilage"));
    }

    [Fact]
    public void RecordSale_QuantityAvailable_DecrementsStockAndCreatesLedger()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        var ledger = item.RecordSale(3m, Guid.NewGuid(), Guid.NewGuid(), Now);
        Assert.Equal(7m, item.CurrentQuantity);
        Assert.Equal(-3m, ledger.QuantityDelta);
        Assert.Equal(InventoryTransactionType.POS_SALE, ledger.TransactionType);
    }

    [Fact]
    public void RecordSale_QuantityExceedsAvailable_ThrowsInvalidOperationException()
    {
        var item = CreateInventory().AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        item.UpdateReservedQuantity(8m);
        Assert.Throws<InvalidOperationException>(() =>
            item.RecordSale(3m, Guid.NewGuid(), Guid.NewGuid(), Now));
    }

    private static DomainInventory CreateInventory()
        => DomainInventory.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
}
