using Haggly.Domain.Modules.Inventory;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Domain.Modules.Inventory;

public sealed class InventoryDirectSaleTests
{
    [Fact]
    public void RecordSaleDirectly_AvailableQuantity_ConsumesStockAndCreatesLedger()
    {
        // Arrange
        var item = CreateItem(10m);

        // Act
        var ledger = item.RecordSaleDirectly(3m, SaleId, ActorId, OccurredAt);

        // Assert
        Assert.Equal(7m, item.CurrentQuantity);
        Assert.Equal(1, item.Version);
        Assert.Equal(InventoryTransactionType.POS_SALE, ledger.TransactionType);
        Assert.Equal(-3m, ledger.QuantityDelta);
        Assert.Equal(SaleId, ledger.ReferenceId);
    }

    [Fact]
    public void RecordSaleDirectly_QuantityExceedsAvailability_RejectsWithoutMutation()
    {
        // Arrange
        var item = CreateItem(10m);
        item.Reserve(8m, OccurredAt);
        var version = item.Version;

        // Act
        var action = () => item.RecordSaleDirectly(3m, SaleId, ActorId, OccurredAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(version, item.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RecordSaleDirectly_NonPositiveQuantity_RejectsWithoutMutation(decimal quantity)
    {
        // Arrange
        var item = CreateItem(10m);

        // Act
        var action = () => item.RecordSaleDirectly(quantity, SaleId, ActorId, OccurredAt);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(0, item.Version);
    }

    private static InventoryItem CreateItem(decimal quantity)
        => DomainInventory.Create(InventoryId, ActorId, OccurredAt)
            .AddItem(ProductStallId, quantity, ActorId, OccurredAt);

    private static readonly Guid InventoryId = Guid.Parse("21000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductStallId = Guid.Parse("21000000-0000-0000-0000-000000000002");
    private static readonly Guid SaleId = Guid.Parse("21000000-0000-0000-0000-000000000003");
    private static readonly Guid ActorId = Guid.Parse("21000000-0000-0000-0000-000000000004");
    private static readonly DateTimeOffset OccurredAt = new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
}
