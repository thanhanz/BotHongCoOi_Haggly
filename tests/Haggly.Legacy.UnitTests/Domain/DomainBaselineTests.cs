using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Domain;

public sealed class DomainBaselineTests
{
    [Fact]
    public void RecalculateTotal_WhenFulfillmentContainsCancelledItems_UsesOnlyActiveItems()
    {
        var fulfillment = new StallFulfillment();
        var activeItem = new OrderItem();
        activeItem.SetFinalValues(quantity: 2, unitPrice: 12_500, isNegotiated: false);
        var cancelledItem = new OrderItem { Status = OrderItemStatus.CANCELLED };
        cancelledItem.SetFinalValues(quantity: 1, unitPrice: 50_000, isNegotiated: false);
        fulfillment.OrderItems.Add(activeItem);
        fulfillment.OrderItems.Add(cancelledItem);

        var order = new Order();
        order.StallFulfillments.Add(fulfillment);
        order.RecalculateTotal();

        Assert.Equal(25_000, order.TotalToCharge);
    }

    [Fact]
    public void Reserve_WhenReservedQuantityExceedsCurrentQuantity_ThrowsInvalidOperationException()
    {
        var inventory = Inventory.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var item = inventory.AddItem(Guid.NewGuid(), 3m, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            item.Reserve(4, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SetFinalValues_WhenQuantityIsNonPositive_ThrowsArgumentOutOfRangeException()
    {
        var item = new OrderItem();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => item.SetFinalValues(quantity: 0, unitPrice: 100, isNegotiated: false));
    }
}
