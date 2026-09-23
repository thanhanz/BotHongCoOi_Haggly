using Haggly.Domain.Modules.Inventory;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Inventory;

public sealed class InventoryReservationTests
{
    [Fact]
    public void Reserve_AvailableQuantity_UpdatesReservedAndAvailableQuantities()
    {
        // Arrange
        var item = CreateItem(10m);

        // Act
        item.Reserve(4m, OccurredAt);

        // Assert
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(4m, item.ReservedQuantity);
        Assert.Equal(6m, item.AvailableQuantity);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void Reserve_AboveAvailability_RejectsWithoutMutation()
    {
        // Arrange
        var item = CreateItem(10m);

        // Act
        var action = () => item.Reserve(11m, OccurredAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(0m, item.ReservedQuantity);
        Assert.Equal(0, item.Version);
    }

    [Fact]
    public void ReleaseReserved_PartOfReservation_UpdatesQuantities()
    {
        // Arrange
        var item = CreateItem(10m);
        item.Reserve(6m, OccurredAt);

        // Act
        item.ReleaseReserved(2m, OccurredAt);

        // Assert
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(4m, item.ReservedQuantity);
        Assert.Equal(6m, item.AvailableQuantity);
        Assert.Equal(2, item.Version);
    }

    [Fact]
    public void ReleaseReserved_AllReservation_ClearsReservation()
    {
        // Arrange
        var item = CreateItem(10m);
        item.Reserve(6m, OccurredAt);

        // Act
        item.ReleaseReserved(6m, OccurredAt);

        // Assert
        Assert.Equal(0m, item.ReservedQuantity);
        Assert.Equal(10m, item.AvailableQuantity);
        Assert.Equal(2, item.Version);
    }

    [Fact]
    public void ReleaseReserved_AboveReservedQuantity_RejectsWithoutMutation()
    {
        // Arrange
        var item = CreateItem(10m);
        item.Reserve(4m, OccurredAt);

        // Act
        var action = () => item.ReleaseReserved(5m, OccurredAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(4m, item.ReservedQuantity);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void ConsumeReservedOnlineSale_ReservedQuantity_ConsumesStockAndReservation()
    {
        // Arrange
        var item = CreateItem(10m);
        item.Reserve(6m, OccurredAt);
        var paymentId = Guid.Parse("30000000-0000-0000-0000-000000000004");

        // Act
        item.ConsumeReservedOnlineSale(4m, paymentId, ConsumedAt);

        // Assert
        Assert.Equal(6m, item.CurrentQuantity);
        Assert.Equal(2m, item.ReservedQuantity);
        Assert.Equal(4m, item.AvailableQuantity);
        Assert.Equal(2, item.Version);
    }

    [Fact]
    public void ConsumeReservedOnlineSale_AboveReservedQuantity_RejectsWithoutMutation()
    {
        // Arrange
        var item = CreateItem(10m);
        item.Reserve(4m, OccurredAt);

        // Act
        var action = () => item.ConsumeReservedOnlineSale(5m, PaymentId, ConsumedAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(10m, item.CurrentQuantity);
        Assert.Equal(4m, item.ReservedQuantity);
        Assert.Equal(1, item.Version);
        Assert.Single(item.InventoryLedgers);
    }

    private static InventoryItem CreateItem(decimal quantity) =>
        DomainInventory.Create(StallId, ActorId, OccurredAt)
            .AddItem(ProductStallId, quantity, ActorId, OccurredAt);

    private static readonly Guid StallId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductStallId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid ActorId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid PaymentId = Guid.Parse("30000000-0000-0000-0000-000000000004");
    private static readonly DateTimeOffset OccurredAt = new(2026, 8, 17, 2, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ConsumedAt = new(2026, 8, 17, 2, 35, 0, TimeSpan.Zero);
}
