using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Sales;

public interface IOrderCatalog
{
    Task<IReadOnlyList<OrderLineSnapshot>> GetOrderLinesAsync(
        IReadOnlyCollection<Guid> inventoryItemIds,
        CancellationToken cancellationToken);
}

public sealed record OrderLineSnapshot(
    Guid InventoryItemId,
    Guid StallId,
    string ProductName,
    ProductUnit SellingUnit,
    decimal UnitPrice,
    decimal AvailableQuantity);
