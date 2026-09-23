using Haggly.Domain.Common;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Domain.Modules.Sales;

public sealed class StallFulfillment : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Guid StallId { get; set; }
    public string FulfillmentNo { get; set; } = string.Empty;
    public StallFulfillmentStatus Status { get; set; } = StallFulfillmentStatus.DRAFT;
    public decimal Subtotal { get; private set; }
    public decimal FinalAmount { get; private set; }
    public decimal PaidAmount { get; set; }
    public string? PickupCode { get; set; }
    public DateTimeOffset? PreparedAt { get; set; }
    public DateTimeOffset? ReadyAt { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public Guid? PickupConfirmedBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public Order? Order { get; set; }
    public Stall? Stall { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    internal static StallFulfillment Create(
        Guid orderId,
        Guid stallId,
        Guid actorId,
        DateTimeOffset occurredAt)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("A valid order ID is required.", nameof(orderId));
        }

        if (stallId == Guid.Empty)
        {
            throw new ArgumentException("A valid stall ID is required.", nameof(stallId));
        }

        return new StallFulfillment
        {
            OrderId = orderId,
            StallId = stallId,
            FulfillmentNo = $"FUL-{orderId:N}-{stallId:N}".ToUpperInvariant(),
            Status = StallFulfillmentStatus.NEGOTIATING,
            CreatedAt = occurredAt,
            CreatedBy = actorId
        };
    }

    internal void Cancel(string reason, DateTimeOffset cancelledAt)
    {
        if (Status is StallFulfillmentStatus.PICKED_UP or StallFulfillmentStatus.CANCELLED)
        {
            throw new InvalidOperationException(
                "A picked-up or cancelled fulfillment cannot be cancelled.");
        }

        Status = StallFulfillmentStatus.CANCELLED;
        CancelledAt = cancelledAt;
        CancellationReason = reason.Trim();
        UpdatedAt = cancelledAt;
        foreach (var item in OrderItems.Where(item => item.Status == OrderItemStatus.ACTIVE))
        {
            item.Status = OrderItemStatus.CANCELLED;
        }

        RecalculateAmounts();
    }

    public void RecalculateAmounts()
    {
        Subtotal = OrderItems
            .Where(item => item.Status == OrderItemStatus.ACTIVE)
            .Sum(item => item.LineTotal);
        FinalAmount = Subtotal;
    }
}
