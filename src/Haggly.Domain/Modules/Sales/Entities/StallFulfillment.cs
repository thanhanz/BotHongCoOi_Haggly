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

    public void RecalculateAmounts()
    {
        Subtotal = OrderItems
            .Where(item => item.Status == OrderItemStatus.ACTIVE)
            .Sum(item => item.LineTotal);
        FinalAmount = Subtotal;
    }
}
