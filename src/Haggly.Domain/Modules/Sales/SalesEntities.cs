using Haggly.Domain.Common;
using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Domain.Modules.Sales;

public sealed class Order : AuditableEntity
{
    public string OrderNo { get; set; } = string.Empty;
    public Guid BuyerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
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
            .Where(item => item.Status == OrderItemStatus.Active)
            .Sum(item => item.LineTotal);
    }
}

public sealed class StallFulfillment : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Guid StallId { get; set; }
    public string FulfillmentNo { get; set; } = string.Empty;
    public StallFulfillmentStatus Status { get; set; } = StallFulfillmentStatus.Draft;
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
            .Where(item => item.Status == OrderItemStatus.Active)
            .Sum(item => item.LineTotal);
        FinalAmount = Subtotal;
    }
}

public sealed class OrderItem : AuditableEntity
{
    public Guid StallFulfillmentId { get; set; }
    public Guid DailyProductListingId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string SellingUnitSnapshot { get; set; } = string.Empty;
    public decimal PublicUnitPriceSnapshot { get; set; }
    public decimal FinalUnitPrice { get; private set; }
    public decimal FinalQuantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public bool IsNegotiated { get; private set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Active;
    public string? Notes { get; set; }

    public StallFulfillment? StallFulfillment { get; set; }
    public DailyProductListing? DailyProductListing { get; set; }
    public ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public void SetFinalValues(decimal quantity, decimal unitPrice, bool isNegotiated)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Final quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Final unit price cannot be negative.");
        }

        FinalQuantity = quantity;
        FinalUnitPrice = unitPrice;
        LineTotal = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
        IsNegotiated = isNegotiated;
    }
}
