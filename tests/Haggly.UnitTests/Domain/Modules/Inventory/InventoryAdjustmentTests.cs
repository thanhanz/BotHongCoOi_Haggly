using Haggly.Domain.Modules.Inventory;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Inventory;

public sealed class InventoryAdjustmentTests
{
    [Fact]
    public void AdjustQuantity_PositiveDelta_UpdatesQuantityAndVersion()
    {
        // Arrange
        var item = CreateItem(10m);

        // Act
        item.AdjustQuantity(3m, ActorId, AdjustmentAt, "delivery");

        // Assert
        Assert.Equal(13m, item.CurrentQuantity);
        Assert.Equal(0m, item.ReservedQuantity);
        Assert.Equal(13m, item.AvailableQuantity);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void AdjustQuantity_NegativeDelta_UpdatesQuantityAndLedger()
    {
        // Arrange
        var item = CreateItem(10m);

        // Act
        var ledger = item.AdjustQuantity(-3m, ActorId, AdjustmentAt, "waste");

        // Assert
        Assert.Equal(7m, item.CurrentQuantity);
        Assert.Equal(7m, item.AvailableQuantity);
        Assert.Equal(1, item.Version);
        Assert.Equal(InventoryTransactionType.ADJUSTMENT, ledger.TransactionType);
    }

    [Fact]
    public void AdjustQuantity_BelowReservedQuantity_RejectsAndLeavesStateUnchanged()
    {
        // Arrange
        var item = CreateItem(10m);
        item.Reserve(4m, ReservationAt);

        // Act
        var action = () => item.AdjustQuantity(-7m, ActorId, AdjustmentAt, "loss");

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(4m, item.ReservedQuantity);
        Assert.Equal(1, item.Version);
        Assert.Single(item.InventoryLedgers);
    }

    [Fact]
    public void AdjustQuantity_ZeroDelta_RejectsWithoutMutation()
    {
        // Arrange
        var item = CreateItem(10m);

        // Act
        var action = () => item.AdjustQuantity(0m, ActorId, AdjustmentAt, "invalid");

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(0, item.Version);
        Assert.Single(item.InventoryLedgers);
    }

    [Fact]
    public void AdjustQuantity_BelowZeroQuantity_RejectsWithoutMutation()
    {
        // Arrange
        var item = CreateItem(10m);

        // Act
        var action = () => item.AdjustQuantity(-11m, ActorId, AdjustmentAt, "loss");

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(0, item.Version);
        Assert.Single(item.InventoryLedgers);
    }

    private static InventoryItem CreateItem(decimal quantity) =>
        DomainInventory.Create(StallId, ActorId, OccurredAt)
            .AddItem(ProductStallId, quantity, ActorId, OccurredAt);

    private static readonly Guid StallId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductStallId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    private static readonly Guid ActorId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset OccurredAt = new(2026, 8, 17, 2, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReservationAt = new(2026, 8, 17, 2, 31, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AdjustmentAt = new(2026, 8, 17, 2, 32, 0, TimeSpan.Zero);
}
