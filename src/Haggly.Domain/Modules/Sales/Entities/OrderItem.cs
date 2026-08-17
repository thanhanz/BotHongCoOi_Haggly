using Haggly.Domain.Common;
using Haggly.Domain.Modules.Inventory;

namespace Haggly.Domain.Modules.Sales;

public sealed class OrderItem : AuditableEntity
{
    public Guid StallFulfillmentId { get; set; }
    public Guid InventoryItemId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string SellingUnitSnapshot { get; set; } = string.Empty;
    public decimal PublicUnitPriceSnapshot { get; set; }
    public decimal FinalUnitPrice { get; private set; }
    public decimal FinalQuantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public bool IsNegotiated { get; private set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.ACTIVE;
    public string? Notes { get; set; }

    public StallFulfillment? StallFulfillment { get; set; }
    public InventoryItem? InventoryItem { get; set; }
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
