using Haggly.Domain.Common;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Domain.Modules.Inventory;

public sealed class InventoryItem : AuditableEntity
{
    public Guid InventoryId { get; private set; }
    public Guid ProductStallId { get; private set; }
    public decimal CurrentQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AvailableQuantity => CurrentQuantity - ReservedQuantity;
    public long Version { get; private set; }

    public Inventory? Inventory { get; set; }
    public ProductStall? ProductStall { get; set; }
    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
    public ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    internal static InventoryItem Create(
        Guid inventoryId,
        Guid productStallId,
        decimal currentQuantity,
        Guid actorId,
        DateTimeOffset occurredAt)
    {
        if (inventoryId == Guid.Empty)
        {
            throw new ArgumentException("A valid inventory ID is required.", nameof(inventoryId));
        }

        if (productStallId == Guid.Empty)
        {
            throw new ArgumentException("A valid stall product ID is required.", nameof(productStallId));
        }

        ValidateNonNegative(currentQuantity, nameof(currentQuantity));

        var item = new InventoryItem
        {
            InventoryId = inventoryId,
            ProductStallId = productStallId,
            CurrentQuantity = currentQuantity,
            CreatedAt = occurredAt,
            CreatedBy = actorId
        };

        item.InventoryLedgers.Add(InventoryLedger.CreateInitialStockEntry(
            item.Id,
            inventoryId,
            currentQuantity,
            actorId,
            occurredAt));
        return item;
    }

    public InventoryLedger AdjustQuantity(
        decimal quantityDelta,
        Guid actorId,
        DateTimeOffset occurredAt,
        string reason)
    {
        if (quantityDelta == 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityDelta), "Quantity delta must not be zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var quantityBefore = CurrentQuantity;
        var quantityAfter = quantityBefore + quantityDelta;
        if (quantityAfter < ReservedQuantity)
        {
            throw new InvalidOperationException("Adjusted quantity cannot be less than reserved quantity.");
        }

        CurrentQuantity = quantityAfter;
        MarkChanged(actorId, occurredAt);
        var ledger = InventoryLedger.CreateAdjustment(
            Id, InventoryId, quantityDelta, quantityBefore, quantityAfter, actorId, occurredAt, reason);
        InventoryLedgers.Add(ledger);
        return ledger;
    }

    public InventoryLedger RecordSale(
        decimal quantity,
        Guid saleId,
        Guid actorId,
        DateTimeOffset occurredAt)
    {
        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Sale quantity must be greater than zero.");
        }

        if (saleId == Guid.Empty)
        {
            throw new ArgumentException("A valid sale ID is required.", nameof(saleId));
        }

        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException("Sale quantity cannot exceed available inventory.");
        }

        var quantityBefore = CurrentQuantity;
        CurrentQuantity -= quantity;
        MarkChanged(actorId, occurredAt);
        var ledger = InventoryLedger.CreateSale(
            Id, InventoryId, quantity, quantityBefore, CurrentQuantity, saleId, actorId, occurredAt);
        InventoryLedgers.Add(ledger);
        return ledger;
    }

    public void UpdateReservedQuantity(decimal reservedQuantity)
    {
        ValidateNonNegative(reservedQuantity, nameof(reservedQuantity));
        if (reservedQuantity > CurrentQuantity)
        {
            throw new InvalidOperationException("Reserved quantity cannot exceed current quantity.");
        }

        if (ReservedQuantity == reservedQuantity)
        {
            return;
        }

        ReservedQuantity = reservedQuantity;
        Version++;
    }

    private void MarkChanged(Guid actorId, DateTimeOffset occurredAt)
    {
        Version++;
        UpdatedAt = occurredAt;
        UpdatedBy = actorId;
    }

    private static void ValidateNonNegative(decimal value, string parameterName)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Inventory quantities cannot be negative.");
        }
    }
}
