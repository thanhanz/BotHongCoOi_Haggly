using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using Xunit;
using DomainOrder = Haggly.Domain.Modules.Sales.Order;

namespace Haggly.UnitTests.Domain.Modules.Sales.Order;

public sealed class OrderCancellationTests
{
    [Fact]
    public void Cancel_NegotiatingOrder_CancelsOrderAndFulfillment()
    {
        // Arrange
        var placedAt = new DateTimeOffset(2026, 8, 17, 2, 0, 0, TimeSpan.Zero);
        var cancelledAt = placedAt.AddMinutes(5);
        var buyerId = Guid.Parse("21000000-0000-0000-0000-000000000002");
        var order = DomainOrder.Place(
            Guid.Parse("21000000-0000-0000-0000-000000000001"), buyerId,
            [new OrderItemInput(Guid.Parse("21000000-0000-0000-0000-000000000003"), Guid.Parse("21000000-0000-0000-0000-000000000004"), "Tomato", ProductUnit.KG, 45_000m, 1m, null)],
            placedAt);

        // Act
        order.Cancel("Buyer changed their mind", cancelledAt);

        // Assert
        Assert.Equal(OrderStatus.CANCELLED, order.Status);
        Assert.Equal("Buyer changed their mind", order.CancellationReason);
        Assert.Equal(StallFulfillmentStatus.CANCELLED, Assert.Single(order.StallFulfillments).Status);
    }

    [Fact]
    public void Cancel_PaidOrder_RejectsCancellation()
    {
        // Arrange
        var order = DomainOrder.Place(
            Guid.Parse("21000000-0000-0000-0000-000000000001"),
            Guid.Parse("21000000-0000-0000-0000-000000000002"),
            [new OrderItemInput(Guid.Parse("21000000-0000-0000-0000-000000000003"), Guid.Parse("21000000-0000-0000-0000-000000000004"), "Tomato", ProductUnit.KG, 45_000m, 1m, null)],
            new DateTimeOffset(2026, 8, 17, 2, 0, 0, TimeSpan.Zero));
        order.Status = OrderStatus.PAID;

        // Act
        var action = () => order.Cancel("Too late", new DateTimeOffset(2026, 8, 17, 2, 5, 0, TimeSpan.Zero));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }
}
