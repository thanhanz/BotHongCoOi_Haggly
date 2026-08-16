using Haggly.Domain.Common;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Domain.Modules.Sales;

public sealed class PosSaleItem : AuditableEntity
{
    public Guid PosSaleId { get; private set; }
    public Guid DailyProductListingId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public ProductUnit SellingUnitSnapshot { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    public PosSale? PosSale { get; private set; }
    
    private PosSaleItem()
    {
    }

    internal static PosSaleItem Create(
        Guid dailyProductListingId,
        string productNameSnapshot,
        ProductUnit sellingUnitSnapshot,
        decimal unitPrice,
        decimal quantity,
        DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productNameSnapshot);
        if (dailyProductListingId == Guid.Empty)
        {
            throw new ArgumentException("A valid daily product listing ID is required.", nameof(dailyProductListingId));
        }

        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (unitPrice < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        return new PosSaleItem
        {
            DailyProductListingId = dailyProductListingId,
            ProductNameSnapshot = productNameSnapshot,
            SellingUnitSnapshot = sellingUnitSnapshot,
            UnitPrice = unitPrice,
            Quantity = quantity,
            LineTotal = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero),
            CreatedAt = occurredAt
        };
    }

}
