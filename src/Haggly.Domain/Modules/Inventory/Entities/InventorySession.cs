using Haggly.Domain.Common;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Domain.Modules.Inventory;

public sealed class InventorySession : AuditableEntity
{
    public Guid StallId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public Guid OpenedBy { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public InventorySessionStatus Status { get; set; } = InventorySessionStatus.OPEN;
    public string? Notes { get; set; }

    public Stall? Stall { get; set; }
    public ICollection<DailyProductListing> DailyProductListings { get; set; } = new List<DailyProductListing>();
    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
}
