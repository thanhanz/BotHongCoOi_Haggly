using Haggly.Domain.Modules.Inventory;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.Application.Abstractions.Inventory;

public interface IInventoryCommandRepository
{
    Task<DomainInventory?> FindInventoryAsync(Guid stallId, CancellationToken cancellationToken);
    Task<InventoryItem?> FindItemAsync(Guid stallId, Guid inventoryItemId, CancellationToken cancellationToken);
    Task<bool> ItemExistsAsync(Guid inventoryId, Guid productStallId, CancellationToken cancellationToken);
    Task AddInventoryAsync(DomainInventory inventory, CancellationToken cancellationToken);
    Task AddItemAsync(InventoryItem item, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
