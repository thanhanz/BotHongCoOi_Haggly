using Haggly.Domain.Common;
using Haggly.Domain.Modules.Catalog;
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

    internal static OrderItem Create(
        Guid fulfillmentId,
        OrderItemInput input,
        DateTimeOffset occurredAt,
        Guid actorId)
    {
        if (fulfillmentId == Guid.Empty)
        {
            throw new ArgumentException("A valid fulfillment ID is required.", nameof(fulfillmentId));
        }

        if (input.InventoryItemId == Guid.Empty)
        {
            throw new ArgumentException("A valid inventory item ID is required.", nameof(input));
        }

        if (input.StallId == Guid.Empty)
        {
            throw new ArgumentException("A valid stall ID is required.", nameof(input));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(input.ProductNameSnapshot);
        if (!Enum.IsDefined(input.SellingUnit))
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Selling unit is invalid.");
        }

        if (input.UnitPrice < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Unit price cannot be negative.");
        }

        if (input.Quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Quantity must be greater than zero.");
        }

        var item = new OrderItem
        {
            StallFulfillmentId = fulfillmentId,
            InventoryItemId = input.InventoryItemId,
            ProductNameSnapshot = input.ProductNameSnapshot.Trim(),
            SellingUnitSnapshot = input.SellingUnit.ToString(),
            PublicUnitPriceSnapshot = input.UnitPrice,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            CreatedAt = occurredAt,
            CreatedBy = actorId
        };
        item.SetFinalValues(input.Quantity, input.UnitPrice, false);
        return item;
    }

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

public sealed record OrderItemInput(
    Guid InventoryItemId,
    Guid StallId,
    string ProductNameSnapshot,
    ProductUnit SellingUnit,
    decimal UnitPrice,
    decimal Quantity,
    string? Notes);
