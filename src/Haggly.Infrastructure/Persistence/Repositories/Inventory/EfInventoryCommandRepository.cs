using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Haggly.Infrastructure.Persistence.Repositories.Inventory;

public sealed class EfInventoryCommandRepository(HagglyDbContext dbContext)
    : IInventoryCommandRepository
{
    public Task<InventorySession?> FindSessionAsync(
        Guid stallId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
        => dbContext.InventorySessions
            .Include(session => session.DailyProductListings)
            .ThenInclude(listing => listing.InventoryLedgers)
            .SingleOrDefaultAsync(
                session => session.StallId == stallId
                    && session.BusinessDate == businessDate,
                cancellationToken);

    public Task<DailyProductListing?> FindListingAsync(
        Guid stallId,
        Guid listingId,
        CancellationToken cancellationToken)
        => dbContext.DailyProductListings
            .Include(listing => listing.InventorySession)
            .Include(listing => listing.InventoryLedgers)
            .SingleOrDefaultAsync(
                listing => listing.Id == listingId
                    && listing.InventorySession!.StallId == stallId,
                cancellationToken);

    public Task<bool> ListingExistsAsync(
        Guid inventorySessionId,
        Guid productStallId,
        CancellationToken cancellationToken)
        => dbContext.DailyProductListings.AnyAsync(
            listing => listing.InventorySessionId == inventorySessionId
                && listing.ProductStallId == productStallId,
            cancellationToken);

    public Task AddSessionAsync(
        InventorySession session,
        CancellationToken cancellationToken)
    {
        dbContext.InventorySessions.Add(session);
        return Task.CompletedTask;
    }

    public Task AddListingAsync(
        DailyProductListing listing,
        CancellationToken cancellationToken)
    {
        dbContext.DailyProductListings.Add(listing);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConflictException(
                "The inventory record was changed by another request. Refresh and retry.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new InventoryConflictException(
                "The inventory record already exists.");
        }
    }
}
