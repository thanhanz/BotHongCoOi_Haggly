using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Inventory;

public sealed class InventoryLedger : ImmutableEntity
{
    public Guid DailyProductListingId { get; private set; }
    public Guid InventorySessionId { get; private set; }
    public InventoryTransactionType TransactionType { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public decimal QuantityBefore { get; private set; }
    public decimal QuantityAfter { get; private set; }
    public decimal? UnitPriceBefore { get; private set; }
    public decimal? UnitPriceAfter { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid? ReferenceId { get; private set; }
    public string? Reason { get; private set; }
    public Guid? PerformedBy { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; } = DateTimeOffset.UtcNow;

    public DailyProductListing? DailyProductListing { get; private set; }
    public InventorySession? InventorySession { get; private set; }

    internal static InventoryLedger CreateOpeningStockEntry(
        Guid dailyProductListingId,
        Guid inventorySessionId,
        decimal quantityAfter,
        decimal unitPriceAfter,
        Guid performedBy,
        DateTimeOffset occurredAt)
        => new()
        {
            DailyProductListingId = dailyProductListingId,
            InventorySessionId = inventorySessionId,
            TransactionType = InventoryTransactionType.OPENING,
            QuantityDelta = quantityAfter,
            QuantityBefore = 0m,
            QuantityAfter = quantityAfter,
            UnitPriceAfter = unitPriceAfter,
            ReferenceType = nameof(InventorySession),
            PerformedBy = performedBy,
            OccurredAt = occurredAt
        };

    internal static InventoryLedger CreateAdjustment(
        Guid dailyProductListingId,
        Guid inventorySessionId,
        decimal quantityDelta,
        decimal quantityBefore,
        decimal quantityAfter,
        Guid performedBy,
        DateTimeOffset occurredAt,
        string reason)
        => new()
        {
            DailyProductListingId = dailyProductListingId,
            InventorySessionId = inventorySessionId,
            TransactionType = InventoryTransactionType.ADJUSTMENT,
            QuantityDelta = quantityDelta,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            ReferenceType = nameof(DailyProductListing),
            Reason = reason,
            PerformedBy = performedBy,
            OccurredAt = occurredAt
        };

    internal static InventoryLedger CreatePriceChange(
        Guid dailyProductListingId,
        Guid inventorySessionId,
        decimal quantity,
        decimal unitPriceBefore,
        decimal unitPriceAfter,
        Guid performedBy,
        DateTimeOffset occurredAt)
        => new()
        {
            DailyProductListingId = dailyProductListingId,
            InventorySessionId = inventorySessionId,
            TransactionType = InventoryTransactionType.PRICE_CHANGE,
            QuantityBefore = quantity,
            QuantityAfter = quantity,
            UnitPriceBefore = unitPriceBefore,
            UnitPriceAfter = unitPriceAfter,
            ReferenceType = nameof(DailyProductListing),
            PerformedBy = performedBy,
            OccurredAt = occurredAt
        };
}
