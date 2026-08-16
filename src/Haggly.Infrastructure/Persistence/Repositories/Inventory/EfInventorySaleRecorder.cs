using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Inventory;

public sealed class EfInventorySaleRecorder(HagglyDbContext dbContext)
    : IInventorySaleRecorder
{
    public async Task<IReadOnlyList<InventorySaleItemSnapshot>> RecordPosSaleAsync(
        Guid stallId,
        Guid saleId,
        Guid actorId,
        IReadOnlyCollection<InventorySaleLine> lines,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var listingIds = lines.Select(line => line.DailyProductListingId).ToArray();
        var listings = await dbContext.DailyProductListings
            .Include(listing => listing.InventorySession)
            .Where(listing => listing.InventorySession!.StallId == stallId
                && listingIds.Contains(listing.Id))
            .ToDictionaryAsync(listing => listing.Id, cancellationToken);

        var snapshots = new List<InventorySaleItemSnapshot>(lines.Count);
        foreach (var line in lines)
        {
            if (!listings.TryGetValue(line.DailyProductListingId, out var listing))
            {
                throw new PosSaleNotFoundException("The daily product listing was not found.");
            }

            try
            {
                listing.InventorySession!.EnsureOpen();
            }
            catch (InvalidOperationException exception)
            {
                throw new PosSaleConflictException(exception.Message);
            }

            if (listing.Version != line.ExpectedVersion)
            {
                throw new PosSaleConflictException(
                    "The listing was changed by another request. Refresh and retry.");
            }

            try
            {
                listing.RecordPosSale(line.Quantity, saleId, actorId, occurredAt);
            }
            catch (InvalidOperationException exception)
            {
                throw new PosSaleConflictException(exception.Message);
            }

            snapshots.Add(new InventorySaleItemSnapshot(
                listing.Id,
                listing.ProductNameSnapshot,
                listing.SellingUnitSnapshot,
                listing.PublicUnitPrice,
                line.Quantity,
                listing.Version));
        }

        return snapshots;
    }
}
