using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Inventory;

public sealed class InventoryLedger : ImmutableEntity
{
    public Guid DailyProductListingId { get; set; }
    public Guid InventorySessionId { get; set; }
    public InventoryTransactionType TransactionType { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal QuantityAfter { get; set; }
    public decimal? UnitPriceBefore { get; set; }
    public decimal? UnitPriceAfter { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string? Reason { get; set; }
    public Guid? PerformedBy { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public DailyProductListing? DailyProductListing { get; set; }
    public InventorySession? InventorySession { get; set; }
}
