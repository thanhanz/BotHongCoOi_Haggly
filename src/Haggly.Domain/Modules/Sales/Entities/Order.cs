using Haggly.Domain.Common;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Domain.Modules.Sales;

public sealed class Order : AuditableEntity
{
    public string OrderNo { get; set; } = string.Empty;
    public Guid BuyerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.DRAFT;
    public decimal TotalToCharge { get; private set; }
    public decimal TotalPaid { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTimeOffset? PlacedAt { get; set; }
    public DateTimeOffset? PaymentDueAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public BuyerProfile? Buyer { get; set; }
    public ICollection<StallFulfillment> StallFulfillments { get; set; } = new List<StallFulfillment>();

    public static Order Place(
        Guid id,
        Guid buyerId,
        IReadOnlyCollection<OrderItemInput> items,
        DateTimeOffset placedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A valid order ID is required.", nameof(id));
        }

        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("A valid buyer ID is required.", nameof(buyerId));
        }

        if (items is null || items.Count == 0)
        {
            throw new ArgumentException("At least one order item is required.", nameof(items));
        }

        var order = new Order
        {
            Id = id,
            OrderNo = $"ORD-{id:N}".ToUpperInvariant(),
            BuyerId = buyerId,
            Status = OrderStatus.NEGOTIATING,
            PlacedAt = placedAt,
            CreatedAt = placedAt,
            CreatedBy = buyerId
        };
        var itemIds = new HashSet<Guid>();

        foreach (var group in items.GroupBy(item => item.StallId))
        {
            var fulfillment = StallFulfillment.Create(
                order.Id,
                group.Key,
                buyerId,
                placedAt);

            foreach (var input in group)
            {
                if (!itemIds.Add(input.InventoryItemId))
                {
                    throw new ArgumentException(
                        "An inventory item can occur only once in an order.",
                        nameof(items));
                }

                fulfillment.OrderItems.Add(OrderItem.Create(
                    fulfillment.Id,
                    input,
                    placedAt,
                    buyerId));
            }

            fulfillment.RecalculateAmounts();
            order.StallFulfillments.Add(fulfillment);
        }

        order.RecalculateTotal();
        return order;
    }

    public void Cancel(string reason, DateTimeOffset cancelledAt)
    {
        if (Status is OrderStatus.PAID
            or OrderStatus.PARTIALLY_PICKED_UP
            or OrderStatus.COMPLETED
            or OrderStatus.CANCELLED)
        {
            throw new InvalidOperationException(
                "Only an order that has not been paid or picked up can be cancelled.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (StallFulfillments.Any(fulfillment =>
            fulfillment.Status is StallFulfillmentStatus.PICKED_UP
                or StallFulfillmentStatus.CANCELLED))
        {
            throw new InvalidOperationException(
                "An order with a picked-up or cancelled fulfillment cannot be cancelled.");
        }

        foreach (var fulfillment in StallFulfillments)
        {
            fulfillment.Cancel(reason, cancelledAt);
        }

        Status = OrderStatus.CANCELLED;
        CancelledAt = cancelledAt;
        CancellationReason = reason.Trim();
        UpdatedAt = cancelledAt;
        UpdatedBy = BuyerId;
    }

    public void RecalculateTotal()
    {
        foreach (var fulfillment in StallFulfillments)
        {
            fulfillment.RecalculateAmounts();
        }

        TotalToCharge = StallFulfillments
            .SelectMany(fulfillment => fulfillment.OrderItems)
            .Where(item => item.Status == OrderItemStatus.ACTIVE)
            .Sum(item => item.LineTotal);
    }
}
