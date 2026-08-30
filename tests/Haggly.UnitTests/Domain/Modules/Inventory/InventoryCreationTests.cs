using Haggly.Domain.Modules.Inventory;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Inventory;

public sealed class InventoryCreationTests
{
    [Fact]
    public void Create_ValidInventory_CreatesEmptyInventory()
    {
        // Arrange
        var stallId = Guid.Parse("10000000-0000-0000-0000-000000000001");

        // Act
        var inventory = DomainInventory.Create(stallId, ActorId, OccurredAt);

        // Assert
        Assert.Equal(stallId, inventory.StallId);
        Assert.Equal(ActorId, inventory.CreatedBy);
        Assert.Equal(OccurredAt, inventory.CreatedAt);
        Assert.Empty(inventory.Items);
    }

    [Fact]
    public void Create_MissingStall_RejectsInventory()
    {
        // Arrange

        // Act
        var action = () => DomainInventory.Create(Guid.Empty, ActorId, OccurredAt);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void AddItem_OpeningQuantity_CreatesAvailableStock()
    {
        // Arrange
        var inventory = CreateInventory();

        // Act
        var item = inventory.AddItem(ProductStallId, 10.5m, ActorId, OccurredAt);

        // Assert
        Assert.Equal(10.5m, item.CurrentQuantity);
        Assert.Equal(0m, item.ReservedQuantity);
        Assert.Equal(10.5m, item.AvailableQuantity);
        Assert.Equal(0, item.Version);
    }

    [Fact]
    public void AddItem_DuplicateProductListing_RejectsAndLeavesInventoryUnchanged()
    {
        // Arrange
        var inventory = CreateInventory();
        inventory.AddItem(ProductStallId, 10m, ActorId, OccurredAt);

        // Act
        var action = () => inventory.AddItem(ProductStallId, 5m, ActorId, OccurredAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        var item = Assert.Single(inventory.Items);
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Single(inventory.Items);
    }

    private static DomainInventory CreateInventory() =>
        DomainInventory.Create(StallId, ActorId, OccurredAt);

    private static readonly Guid StallId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductStallId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid ActorId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset OccurredAt = new(2026, 8, 17, 2, 30, 0, TimeSpan.Zero);
}
