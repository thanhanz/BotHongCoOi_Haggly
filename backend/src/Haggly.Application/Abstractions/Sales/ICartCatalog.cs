using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Sales;

public interface ICartCatalog
{
    Task<IReadOnlyList<CartItemSnapshot>> GetItemsAsync(
        IReadOnlyCollection<Guid> inventoryItemIds,
        CancellationToken cancellationToken);
}

public sealed record CartItemSnapshot(
    Guid InventoryItemId,
    Guid ProductStallId,
    Guid StallId,
    string ProductName,
    ProductUnit SellingUnit,
    decimal MinimumOrderQuantity,
    decimal UnitPrice,
    bool IsNegotiable,
    decimal RemainingQuantity,
    bool IsOrderable);
