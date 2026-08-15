using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Modules.Inventory.Dtos;

public sealed record DailyProductListingDto(
    Guid Id,
    Guid InventorySessionId,
    Guid ProductStallId,
    string ProductNameSnapshot,
    ProductUnit SellingUnitSnapshot,
    decimal PublicUnitPrice,
    decimal OpeningQuantity,
    decimal CurrentQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    DailyListingStatus Status,
    long Version)
{
    public static DailyProductListingDto From(DailyProductListing value)
        => new(
            value.Id,
            value.InventorySessionId,
            value.ProductStallId,
            value.ProductNameSnapshot,
            value.SellingUnitSnapshot,
            value.PublicUnitPrice,
            value.OpeningQuantity,
            value.CurrentQuantity,
            value.ReservedQuantity,
            value.AvailableQuantity,
            value.Status,
            value.Version);
}
