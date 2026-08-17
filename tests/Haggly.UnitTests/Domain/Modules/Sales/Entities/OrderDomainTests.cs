using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Sales.Entities;

public sealed class OrderDomainTests
{
    private static readonly DateTimeOffset PlacedAt =
        new(2026, 8, 17, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Place_WithItemsFromMultipleStalls_CreatesOneFulfillmentPerStall()
    {
        var buyerId = Guid.NewGuid();
        var firstStallId = Guid.NewGuid();
        var secondStallId = Guid.NewGuid();

        var order = Order.Place(
            Guid.NewGuid(),
            buyerId,
            [
                new OrderItemInput(Guid.NewGuid(), firstStallId, "Tomato", ProductUnit.KG, 45_000m, 2m, null),
                new OrderItemInput(Guid.NewGuid(), secondStallId, "Fish", ProductUnit.KG, 120_000m, 1.5m, "Cleaned")
            ],
            PlacedAt);

        Assert.Equal(buyerId, order.BuyerId);
        Assert.Equal("VND", order.Currency);
        Assert.Equal("ORD-" + order.Id.ToString("N").ToUpperInvariant(), order.OrderNo);
        Assert.Equal(OrderStatus.NEGOTIATING, order.Status);
        Assert.Equal(270_000m, order.TotalToCharge);
        Assert.Equal(PlacedAt, order.PlacedAt);
        Assert.Equal(2, order.StallFulfillments.Count);

        var firstFulfillment = Assert.Single(order.StallFulfillments, item => item.StallId == firstStallId);
        Assert.Equal(90_000m, firstFulfillment.Subtotal);
        Assert.Equal(StallFulfillmentStatus.NEGOTIATING, firstFulfillment.Status);
        Assert.Equal(2m, Assert.Single(firstFulfillment.OrderItems).FinalQuantity);
    }

    [Fact]
    public void Place_WithDuplicateInventoryItemIds_ThrowsArgumentException()
    {
        var inventoryItemId = Guid.NewGuid();
        var line = new OrderItemInput(
            inventoryItemId, Guid.NewGuid(), "Tomato", ProductUnit.KG, 45_000m, 1m, null);

        Assert.Throws<ArgumentException>(() => Order.Place(
            Guid.NewGuid(), Guid.NewGuid(), [line, line], PlacedAt));
    }

    [Fact]
    public void Cancel_WhenOrderIsNegotiating_CancelsOrderAndActiveFulfillments()
    {
        var order = Order.Place(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new OrderItemInput(
                Guid.NewGuid(), Guid.NewGuid(), "Tomato", ProductUnit.KG, 45_000m, 1m, null)],
            PlacedAt);

        order.Cancel("Buyer changed their mind", PlacedAt.AddMinutes(5));

        Assert.Equal(OrderStatus.CANCELLED, order.Status);
        Assert.Equal("Buyer changed their mind", order.CancellationReason);
        Assert.Equal(PlacedAt.AddMinutes(5), order.CancelledAt);
        var fulfillment = Assert.Single(order.StallFulfillments);
        Assert.Equal(StallFulfillmentStatus.CANCELLED, fulfillment.Status);
        Assert.Equal(OrderItemStatus.CANCELLED, Assert.Single(fulfillment.OrderItems).Status);
    }

    [Fact]
    public void Cancel_WhenOrderIsPaid_ThrowsInvalidOperationException()
    {
        var order = Order.Place(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new OrderItemInput(
                Guid.NewGuid(), Guid.NewGuid(), "Tomato", ProductUnit.KG, 45_000m, 1m, null)],
            PlacedAt);
        order.Status = OrderStatus.PAID;

        Assert.Throws<InvalidOperationException>(() =>
            order.Cancel("Too late", PlacedAt.AddMinutes(5)));
    }
}
