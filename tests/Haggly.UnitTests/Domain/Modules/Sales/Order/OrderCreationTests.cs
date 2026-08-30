using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using Xunit;
using DomainOrder = Haggly.Domain.Modules.Sales.Order;

namespace Haggly.UnitTests.Domain.Modules.Sales.Order;

public sealed class OrderCreationTests
{
    [Fact]
    public void Place_ItemsFromMultipleStalls_CreatesFulfillmentsAndCalculatesTotal()
    {
        // Arrange
        var orderId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var buyerId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var firstStallId = Guid.Parse("20000000-0000-0000-0000-000000000003");
        var secondStallId = Guid.Parse("20000000-0000-0000-0000-000000000004");
        var placedAt = new DateTimeOffset(2026, 8, 17, 2, 0, 0, TimeSpan.Zero);

        // Act
        var order = DomainOrder.Place(orderId, buyerId,
        [
            new OrderItemInput(Guid.Parse("20000000-0000-0000-0000-000000000005"), firstStallId, "Tomato", ProductUnit.KG, 45_000m, 2m, null),
            new OrderItemInput(Guid.Parse("20000000-0000-0000-0000-000000000006"), secondStallId, "Fish", ProductUnit.KG, 120_000m, 1.5m, "Cleaned")
        ], placedAt);

        // Assert
        Assert.Equal(OrderStatus.NEGOTIATING, order.Status);
        Assert.Equal(270_000m, order.TotalToCharge);
        Assert.Equal(2, order.StallFulfillments.Count);
    }

    [Fact]
    public void Place_DuplicateInventoryItemIds_RejectsOrder()
    {
        // Arrange
        var item = new OrderItemInput(
            Guid.Parse("20000000-0000-0000-0000-000000000005"),
            Guid.Parse("20000000-0000-0000-0000-000000000003"),
            "Tomato", ProductUnit.KG, 45_000m, 1m, null);

        // Act
        Action action = () =>
        {
            _ = DomainOrder.Place(
                Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Guid.Parse("20000000-0000-0000-0000-000000000002"),
                [item, item],
                new DateTimeOffset(2026, 8, 17, 2, 0, 0, TimeSpan.Zero));
        };

        // Assert
        Assert.Throws<ArgumentException>(action);
    }
}
