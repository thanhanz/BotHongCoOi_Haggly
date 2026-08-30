using Haggly.Domain.Modules.Inventory;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Domain.Modules.Inventory;

public sealed class InventoryTests
{
    [Fact]
    public void Create_ValidStall_CreatesEmptyInventory()
    {
        // Arrange
        var stallId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var actorId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var occurredAt = new DateTimeOffset(2026, 8, 17, 2, 30, 0, TimeSpan.Zero);

        // Act
        var inventory = DomainInventory.Create(stallId, actorId, occurredAt);

        // Assert
        Assert.Equal(stallId, inventory.StallId);
        Assert.Empty(inventory.Items);
    }

    [Fact]
    public void AddItem_InitialQuantity_CreatesAvailableStockAndOpeningLedger()
    {
        // Arrange
        var inventory = CreateInventory();
        var productStallId = Guid.Parse("10000000-0000-0000-0000-000000000003");

        // Act
        var item = inventory.AddItem(productStallId, 10.5m, ActorId, OccurredAt);

        // Assert
        Assert.Equal(10.5m, item.CurrentQuantity);
        Assert.Equal(10.5m, item.AvailableQuantity);
        Assert.Equal(InventoryTransactionType.OPENING, Assert.Single(item.InventoryLedgers).TransactionType);
    }

    [Fact]
    public void AddItem_DuplicateProductStall_RejectsDuplicateStock()
    {
        // Arrange
        var inventory = CreateInventory();
        var productStallId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        inventory.AddItem(productStallId, 1m, ActorId, OccurredAt);

        // Act
        var action = () => inventory.AddItem(productStallId, 1m, ActorId, OccurredAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void Reserve_SufficientAvailability_UpdatesReservedQuantity()
    {
        // Arrange
        var item = CreateInventory().AddItem(
            Guid.Parse("10000000-0000-0000-0000-000000000003"), 10m, ActorId, OccurredAt);

        // Act
        item.Reserve(4m, OccurredAt);

        // Assert
        Assert.Equal(4m, item.ReservedQuantity);
        Assert.Equal(6m, item.AvailableQuantity);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void Reserve_QuantityExceedsAvailability_RejectsReservation()
    {
        // Arrange
        var item = CreateInventory().AddItem(
            Guid.Parse("10000000-0000-0000-0000-000000000003"), 10m, ActorId, OccurredAt);

        // Act
        var action = () => item.Reserve(11m, OccurredAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(0m, item.ReservedQuantity);
    }

    private static readonly Guid ActorId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 17, 2, 30, 0, TimeSpan.Zero);

    private static DomainInventory CreateInventory()
        => DomainInventory.Create(
            Guid.Parse("10000000-0000-0000-0000-000000000001"), ActorId, OccurredAt);
}
