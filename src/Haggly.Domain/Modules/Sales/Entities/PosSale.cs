using Haggly.Domain.Common;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Domain.Modules.Sales;

/// <summary>
///   This entity for in-person Order (buyer -> Anonymous)
/// </summary>

public sealed class PosSale : AuditableEntity
{
    public Guid StallId { get; private set; }
    public string SaleNo { get; private set; } = string.Empty;
    
    //It's durable idempotency for retries.
    public string ClientRequestId { get; private set; } = string.Empty;
    public PosSaleStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Guid CompletedBy { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }

    public ICollection<PosSaleItem> Items { get; private set; } = new List<PosSaleItem>();


    private PosSale()
    {
    }

    public static PosSale Complete(
        Guid id,
        Guid stallId,
        Guid completedBy,
        string clientRequestId,
        IReadOnlyCollection<PosSaleItemInput> items,
        DateTimeOffset completedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A valid sale ID is required.", nameof(id));
        }

        if (stallId == Guid.Empty)
        {
            throw new ArgumentException("A valid stall ID is required.", nameof(stallId));
        }

        if (completedBy == Guid.Empty)
        {
            throw new ArgumentException("A valid actor ID is required.", nameof(completedBy));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(clientRequestId);
        if (items is null || items.Count == 0)
        {
            throw new ArgumentException("At least one sale item is required.", nameof(items));
        }

        var listingIds = new HashSet<Guid>();
        var sale = new PosSale
        {
            Id = id,
            StallId = stallId,
            SaleNo = $"POS-{id:N}".ToUpperInvariant(),
            ClientRequestId = clientRequestId.Trim(),
            Status = PosSaleStatus.COMPLETED,
            CompletedBy = completedBy,
            CompletedAt = completedAt,
            CreatedAt = completedAt,
            CreatedBy = completedBy
        };

        foreach (var item in items)
        {
            if (!listingIds.Add(item.InventoryItemId))
            {
                throw new ArgumentException(
                    "A daily product listing can occur only once in a POS sale.",
                    nameof(items));
            }

            sale.Items.Add(PosSaleItem.Create(
                item.InventoryItemId,
                item.ProductNameSnapshot,
                item.SellingUnitSnapshot,
                item.UnitPrice,
                item.Quantity,
                completedAt));
        }

        sale.TotalAmount = sale.Items.Sum(item => item.LineTotal);
        return sale;
    }
}

public sealed record PosSaleItemInput(
    Guid InventoryItemId,
    string ProductNameSnapshot,
    ProductUnit SellingUnitSnapshot,
    decimal UnitPrice,
    decimal Quantity);
