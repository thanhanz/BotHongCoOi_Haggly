using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Abstractions.Inventory;

public interface IInventoryCommandRepository
{
    Task<InventorySession?> FindSessionAsync(
        Guid stallId,
        DateOnly businessDate,
        CancellationToken cancellationToken);

    Task<DailyProductListing?> FindListingAsync(
        Guid stallId,
        Guid listingId,
        CancellationToken cancellationToken);

    Task<bool> ListingExistsAsync(
        Guid inventorySessionId,
        Guid productStallId,
        CancellationToken cancellationToken);

    Task AddSessionAsync(
        InventorySession session,
        CancellationToken cancellationToken);

    Task AddListingAsync(
        DailyProductListing listing,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
