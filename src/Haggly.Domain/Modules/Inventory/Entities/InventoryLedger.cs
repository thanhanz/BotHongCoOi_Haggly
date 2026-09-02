using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Inventory;

public sealed class InventoryLedger : ImmutableEntity
{
    public Guid InventoryItemId { get; private set; }
    public Guid InventoryId { get; private set; }
    public InventoryTransactionType TransactionType { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public decimal QuantityBefore { get; private set; }
    public decimal QuantityAfter { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid? ReferenceId { get; private set; }
    public string? Reason { get; private set; }
    public Guid? PerformedBy { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; } = DateTimeOffset.UtcNow;

    public InventoryItem? InventoryItem { get; private set; }
    public Inventory? Inventory { get; private set; }

    internal static InventoryLedger CreateInitialStockEntry(
        Guid inventoryItemId,
        Guid inventoryId,
        decimal quantityAfter,
        Guid performedBy,
        DateTimeOffset occurredAt)
        => new()
        {
            InventoryItemId = inventoryItemId,
            InventoryId = inventoryId,
            TransactionType = InventoryTransactionType.OPENING,
            QuantityDelta = quantityAfter,
            QuantityAfter = quantityAfter,
            ReferenceType = nameof(InventoryItem),
            PerformedBy = performedBy,
            OccurredAt = occurredAt,
            CreatedAt = occurredAt,
            CreatedBy = performedBy
        };

    internal static InventoryLedger CreateAdjustment(
        Guid inventoryItemId,
        Guid inventoryId,
        decimal quantityDelta,
        decimal quantityBefore,
        decimal quantityAfter,
        Guid performedBy,
        DateTimeOffset occurredAt,
        string reason)
        => new()
        {
            InventoryItemId = inventoryItemId,
            InventoryId = inventoryId,
            TransactionType = InventoryTransactionType.ADJUSTMENT,
            QuantityDelta = quantityDelta,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            ReferenceType = nameof(InventoryItem),
            Reason = reason,
            PerformedBy = performedBy,
            OccurredAt = occurredAt,
            CreatedAt = occurredAt,
            CreatedBy = performedBy
        };

    internal static InventoryLedger CreateSale(
        Guid inventoryItemId,
        Guid inventoryId,
        decimal quantity,
        decimal quantityBefore,
        decimal quantityAfter,
        Guid saleId,
        Guid performedBy,
        DateTimeOffset occurredAt)
        => new()
        {
            InventoryItemId = inventoryItemId,
            InventoryId = inventoryId,
            TransactionType = InventoryTransactionType.POS_SALE,
            QuantityDelta = -quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            ReferenceType = "POS_SALE",
            ReferenceId = saleId,
            PerformedBy = performedBy,
            OccurredAt = occurredAt,
            CreatedAt = occurredAt,
            CreatedBy = performedBy
        };

    internal static InventoryLedger CreateOnlineSale(
        Guid inventoryItemId,
        Guid inventoryId,
        decimal quantity,
        decimal quantityBefore,
        decimal quantityAfter,
        Guid paymentTransactionId,
        DateTimeOffset occurredAt)
        => new()
        { 
            InventoryItemId = inventoryItemId,
            InventoryId = inventoryId,
            TransactionType = InventoryTransactionType.ONLINE_SALE,
            QuantityDelta = -quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            ReferenceType = "PAYMENT_TRANSACTION",
            ReferenceId = paymentTransactionId,
            OccurredAt = occurredAt.ToUniversalTime(),
            CreatedAt = occurredAt.ToUniversalTime()
        };
}
