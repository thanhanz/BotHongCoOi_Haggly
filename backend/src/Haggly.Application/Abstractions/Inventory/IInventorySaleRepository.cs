using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Inventory;

public interface IInventorySaleRepository
{
    Task<IReadOnlyList<InventorySaleItemSnapshot>> RecordPosSaleAsync(
        Guid stallId,
        Guid saleId,
        Guid actorId,
        IReadOnlyCollection<InventorySaleLine> lines,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);
}

public sealed record InventorySaleLine(
    Guid InventoryItemId,
    decimal Quantity,
    long ExpectedInventoryVersion,
    long ExpectedProductStallVersion);

public sealed record InventorySaleItemSnapshot(
    Guid InventoryItemId,
    string ProductNameSnapshot,
    ProductUnit SellingUnitSnapshot,
    decimal UnitPrice,
    decimal Quantity,
    long InventoryVersion,
    long ProductStallVersion);
