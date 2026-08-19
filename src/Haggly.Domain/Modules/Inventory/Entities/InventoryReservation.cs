using Haggly.Domain.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Inventory;

public sealed class InventoryReservation : AuditableEntity
{
    public Guid InventoryItemId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid StallFulfillmentId { get; set; }
    public decimal ReservedQuantity { get; set; }
    public InventoryReservationStatus Status { get; set; } = InventoryReservationStatus.ACTIVE;
    public DateTimeOffset ReservedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public string? ReleaseReason { get; set; }

    public InventoryItem? InventoryItem { get; set; }
    public OrderItem? OrderItem { get; set; }
    public StallFulfillment? StallFulfillment { get; set; }
}
