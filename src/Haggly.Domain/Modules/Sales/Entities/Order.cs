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

    public void RecalculateTotal()
    {
        TotalToCharge = StallFulfillments
            .SelectMany(fulfillment => fulfillment.OrderItems)
            .Where(item => item.Status == OrderItemStatus.ACTIVE)
            .Sum(item => item.LineTotal);
    }
}
