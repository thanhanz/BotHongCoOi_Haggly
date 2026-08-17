using Haggly.Domain.Common;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Domain.Modules.Inventory;

public sealed class Inventory : AuditableEntity
{
    public Guid StallId { get; private set; }

    public Stall? Stall { get; set; }
    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();

    public static Inventory Create(
        Guid stallId,
        Guid actorId,
        DateTimeOffset occurredAt)
    {
        if (stallId == Guid.Empty)
        {
            throw new ArgumentException("A stall is required to create an inventory.", nameof(stallId));
        }

        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("An actor is required to create an inventory.", nameof(actorId));
        }

        return new Inventory
        {
            StallId = stallId,
            CreatedAt = occurredAt,
            CreatedBy = actorId
        };
    }

    public InventoryItem AddItem(
        Guid productStallId,
        decimal currentQuantity,
        Guid actorId,
        DateTimeOffset occurredAt)
    {
        if (Items.Any(item => item.ProductStallId == productStallId))
        {
            throw new InvalidOperationException("The product already exists in this inventory.");
        }

        var item = InventoryItem.Create(
            Id,
            productStallId,
            currentQuantity,
            actorId,
            occurredAt);
        Items.Add(item);
        UpdatedAt = occurredAt;
        UpdatedBy = actorId;
        return item;
    }
}
