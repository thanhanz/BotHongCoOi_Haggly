using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Inventory;

public interface IInventorySaleRecorder
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
    Guid DailyProductListingId,
    decimal Quantity,
    long ExpectedVersion);

public sealed record InventorySaleItemSnapshot(
    Guid DailyProductListingId,
    string ProductNameSnapshot,
    ProductUnit SellingUnitSnapshot,
    decimal UnitPrice,
    decimal Quantity,
    long Version);
