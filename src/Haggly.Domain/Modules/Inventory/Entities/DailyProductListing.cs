using Haggly.Domain.Common;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Domain.Modules.Inventory;

public sealed class DailyProductListing : AuditableEntity
{
    public Guid InventorySessionId { get; private set; }
    public Guid ProductStallId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public ProductUnit SellingUnitSnapshot { get; private set; }
    public decimal PublicUnitPrice { get; private set; }
    public decimal OpeningQuantity { get; private set; }
    public decimal CurrentQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AvailableQuantity { get; private set; }
    public DailyListingStatus Status { get; private set; } = DailyListingStatus.AVAILABLE;
    public long Version { get; private set; }

    public InventorySession? InventorySession { get; set; }
    public ProductStall? ProductStall { get; set; }
    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();
    public ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public static DailyProductListing Open(
        Guid inventorySessionId,
        Guid productStallId,
        string productNameSnapshot,
        ProductUnit sellingUnitSnapshot,
        decimal publicUnitPrice,
        decimal openingQuantity,
        Guid actorId,
        DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productNameSnapshot);
        ValidateNonNegative(publicUnitPrice, nameof(publicUnitPrice));
        ValidateNonNegative(openingQuantity, nameof(openingQuantity));

        var listing = new DailyProductListing
        {
            InventorySessionId = inventorySessionId,
            ProductStallId = productStallId,
            ProductNameSnapshot = productNameSnapshot,
            SellingUnitSnapshot = sellingUnitSnapshot,
            PublicUnitPrice = publicUnitPrice,
            OpeningQuantity = openingQuantity,
            CurrentQuantity = openingQuantity,
            ReservedQuantity = 0m,
            CreatedAt = occurredAt,
            CreatedBy = actorId
        };

        listing.RefreshAvailableQuantity();
        listing.InventoryLedgers.Add(InventoryLedger.CreateOpeningStockEntry(
            listing.Id,
            inventorySessionId,
            openingQuantity,
            publicUnitPrice,
            actorId,
            occurredAt));

        return listing;
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
        if (quantityAfter < 0m || quantityAfter < ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Adjusted quantity cannot be negative or less than reserved quantity.");
        }

        CurrentQuantity = quantityAfter;
        RefreshAvailableQuantity();
        Version++;
        UpdatedAt = occurredAt;
        UpdatedBy = actorId;

        var ledger = InventoryLedger.CreateAdjustment(
            Id,
            InventorySessionId,
            quantityDelta,
            quantityBefore,
            quantityAfter,
            actorId,
            occurredAt,
            reason);
        InventoryLedgers.Add(ledger);
        return ledger;
    }

    public InventoryLedger ChangePrice(
        decimal publicUnitPrice,
        Guid actorId,
        DateTimeOffset occurredAt)
    {
        ValidateNonNegative(publicUnitPrice, nameof(publicUnitPrice));

        var priceBefore = PublicUnitPrice;
        if (priceBefore == publicUnitPrice)
        {
            throw new InvalidOperationException("The public unit price has not changed.");
        }

        PublicUnitPrice = publicUnitPrice;
        Version++;
        UpdatedAt = occurredAt;
        UpdatedBy = actorId;

        var ledger = InventoryLedger.CreatePriceChange(
            Id,
            InventorySessionId,
            CurrentQuantity,
            priceBefore,
            publicUnitPrice,
            actorId,
            occurredAt);
        InventoryLedgers.Add(ledger);
        return ledger;
    }

    public void Hide()
        => HideCore(null, null);

    public void Hide(Guid actorId, DateTimeOffset occurredAt)
        => HideCore(actorId, occurredAt);

    private void HideCore(Guid? actorId, DateTimeOffset? occurredAt)
    {
        if (Status == DailyListingStatus.HIDDEN)
        {
            return;
        }

        Status = DailyListingStatus.HIDDEN;
        Version++;
        MarkUpdated(actorId, occurredAt);
    }

    public void Show()
        => ShowCore(null, null);

    public void Show(Guid actorId, DateTimeOffset occurredAt)
        => ShowCore(actorId, occurredAt);

    private void ShowCore(Guid? actorId, DateTimeOffset? occurredAt)
    {
        if (Status != DailyListingStatus.HIDDEN)
        {
            return;
        }

        Status = AvailableQuantity == 0m
            ? DailyListingStatus.SOLD_OUT
            : DailyListingStatus.AVAILABLE;
        Version++;
        MarkUpdated(actorId, occurredAt);
    }

    public void UpdateReservedQuantity(decimal reservedQuantity)
    {
        ValidateNonNegative(reservedQuantity, nameof(reservedQuantity));
        if (reservedQuantity > CurrentQuantity)
        {
            throw new InvalidOperationException(
                "Reserved quantity cannot exceed current quantity.");
        }

        if (ReservedQuantity == reservedQuantity)
        {
            return;
        }

        ReservedQuantity = reservedQuantity;
        RefreshAvailableQuantity();
        Version++;
    }

    private void RefreshAvailableQuantity()
    {
        if (CurrentQuantity < 0m || ReservedQuantity < 0m || ReservedQuantity > CurrentQuantity)
        {
            throw new InvalidOperationException(
                "Inventory quantities must be non-negative and reservations cannot exceed current stock.");
        }

        AvailableQuantity = CurrentQuantity - ReservedQuantity;
        if (Status != DailyListingStatus.HIDDEN)
        {
            Status = AvailableQuantity == 0m
                ? DailyListingStatus.SOLD_OUT
                : DailyListingStatus.AVAILABLE;
        }
    }

    private void MarkUpdated(Guid? actorId, DateTimeOffset? occurredAt)
    {
        if (actorId is not null && occurredAt is not null)
        {
            UpdatedBy = actorId;
            UpdatedAt = occurredAt;
        }
    }

    private static void ValidateNonNegative(decimal value, string parameterName)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Inventory values cannot be negative.");
        }
    }
}
