using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.Infrastructure.Persistence.Repositories.Inventory;

public sealed class EfInventoryCommandRepository(HagglyDbContext dbContext) : IInventoryCommandRepository
{
    public Task<DomainInventory?> FindInventoryAsync(Guid stallId, CancellationToken cancellationToken)
        => dbContext.Inventories.Include(inventory => inventory.Items)
            .ThenInclude(item => item.InventoryLedgers)
            .SingleOrDefaultAsync(inventory => inventory.StallId == stallId, cancellationToken);

    public Task<InventoryItem?> FindItemAsync(Guid stallId, Guid inventoryItemId, CancellationToken cancellationToken)
        => dbContext.InventoryItems.Include(item => item.Inventory).Include(item => item.InventoryLedgers)
            .SingleOrDefaultAsync(item => item.Id == inventoryItemId && item.Inventory!.StallId == stallId, cancellationToken);

    public Task<bool> ItemExistsAsync(Guid inventoryId, Guid productStallId, CancellationToken cancellationToken)
        => dbContext.InventoryItems.AnyAsync(
            item => item.InventoryId == inventoryId && item.ProductStallId == productStallId, cancellationToken);

    public Task AddInventoryAsync(DomainInventory inventory, CancellationToken cancellationToken)
    {
        dbContext.Inventories.Add(inventory);
        return Task.CompletedTask;
    }

    public Task AddItemAsync(InventoryItem item, CancellationToken cancellationToken)
    {
        dbContext.InventoryItems.Add(item);
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
            throw new InventoryConflictException("The inventory was changed by another request. Refresh and retry.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new InventoryConflictException("The inventory record already exists.");
        }
    }
}
