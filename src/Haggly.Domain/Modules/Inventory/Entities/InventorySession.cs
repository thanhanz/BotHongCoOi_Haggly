using Haggly.Domain.Common;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Domain.Modules.Inventory;

public sealed class InventorySession : AuditableEntity
{
    public Guid StallId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public Guid OpenedBy { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public InventorySessionStatus Status { get; private set; } = InventorySessionStatus.OPEN;
    public string? Notes { get; private set; }

    public Stall? Stall { get; set; }
    public ICollection<DailyProductListing> DailyProductListings { get; set; } = new List<DailyProductListing>();
    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();

    public static InventorySession Open(
        Guid stallId,
        DateOnly businessDate,
        DateTimeOffset openedAt,
        Guid openedBy,
        string? notes)
    {
        if (stallId == Guid.Empty)
        {
            throw new ArgumentException("A stall is required to open an inventory session.", nameof(stallId));
        }

        if (openedBy == Guid.Empty)
        {
            throw new ArgumentException("An actor is required to open an inventory session.", nameof(openedBy));
        }

        return new InventorySession
        {
            StallId = stallId,
            BusinessDate = businessDate,
            OpenedAt = openedAt,
            OpenedBy = openedBy,
            Notes = notes,
            CreatedAt = openedAt,
            CreatedBy = openedBy,
            Status = InventorySessionStatus.OPEN
        };
    }

    public void Close(Guid closedBy, DateTimeOffset closedAt)
    {
        if (Status != InventorySessionStatus.OPEN)
        {
            throw new InvalidOperationException("Only an open inventory session can be closed.");
        }

        if (closedBy == Guid.Empty)
        {
            throw new ArgumentException("An actor is required to close an inventory session.", nameof(closedBy));
        }

        if (closedAt < OpenedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(closedAt), "A session cannot close before it opens.");
        }

        Status = InventorySessionStatus.CLOSED;
        ClosedAt = closedAt;
        ClosedBy = closedBy;
        UpdatedAt = closedAt;
        UpdatedBy = closedBy;
    }

    public void EnsureOpen()
    {
        if (Status != InventorySessionStatus.OPEN)
        {
            throw new InvalidOperationException("The inventory session is closed.");
        }
    }
}
