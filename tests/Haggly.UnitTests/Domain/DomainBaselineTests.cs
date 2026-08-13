using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Domain;

public sealed class DomainBaselineTests
{
    [Fact]
    public void Order_total_uses_only_active_order_items()
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
    public void Daily_listing_rejects_reservations_above_current_quantity()
    {
        var listing = new DailyProductListing
        {
            CurrentQuantity = 3,
            ReservedQuantity = 4
        };

        Assert.Throws<InvalidOperationException>(listing.RefreshAvailableQuantity);
    }

    [Fact]
    public void Order_item_rejects_non_positive_final_quantity()
    {
        var item = new OrderItem();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => item.SetFinalValues(quantity: 0, unitPrice: 100, isNegotiated: false));
    }
}
