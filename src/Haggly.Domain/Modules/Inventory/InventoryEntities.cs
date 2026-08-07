using Haggly.Domain.Common;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Inventory;

public sealed class InventorySession : AuditableEntity
{
    public Guid StallId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public Guid OpenedBy { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public InventorySessionStatus Status { get; set; } = InventorySessionStatus.Open;
    public string? Notes { get; set; }

    public Stall? Stall { get; set; }
    public ICollection<DailyProductListing> DailyProductListings { get; set; } = new List<DailyProductListing>();
    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
}

public sealed class DailyProductListing : AuditableEntity
{
    public Guid InventorySessionId { get; set; }
    public Guid ProductStallId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public ProductUnit SellingUnitSnapshot { get; set; }
    public decimal PublicUnitPrice { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; private set; }
    public DailyListingStatus Status { get; set; } = DailyListingStatus.Available;
    public long Version { get; private set; }

    public InventorySession? InventorySession { get; set; }
    public ProductStall? ProductStall { get; set; }
    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
    public ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public void RefreshAvailableQuantity()
    {
        if (CurrentQuantity < 0 || ReservedQuantity < 0 || ReservedQuantity > CurrentQuantity)
        {
            throw new InvalidOperationException("Inventory quantities must be non-negative and reservations cannot exceed current stock.");
        }

        AvailableQuantity = CurrentQuantity - ReservedQuantity;
        Status = AvailableQuantity == 0 ? DailyListingStatus.SoldOut : DailyListingStatus.Available;
    }
}

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

public sealed class InventoryReservation : AuditableEntity
{
    public Guid DailyProductListingId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid StallFulfillmentId { get; set; }
    public decimal ReservedQuantity { get; set; }
    public InventoryReservationStatus Status { get; set; } = InventoryReservationStatus.Active;
    public DateTimeOffset ReservedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public string? ReleaseReason { get; set; }

    public DailyProductListing? DailyProductListing { get; set; }
    public OrderItem? OrderItem { get; set; }
    public StallFulfillment? StallFulfillment { get; set; }
}
