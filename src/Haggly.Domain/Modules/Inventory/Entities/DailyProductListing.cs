using Haggly.Domain.Common;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Domain.Modules.Inventory;

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
    public DailyListingStatus Status { get; set; } = DailyListingStatus.AVAILABLE;
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
        Status = AvailableQuantity == 0 ? DailyListingStatus.SOLD_OUT : DailyListingStatus.AVAILABLE;
    }
}
