using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Inventory;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.Application.Abstractions.Inventory;

public interface IInventoryQuery
{
    Task<DomainInventory?> GetInventoryAsync(Guid stallId, CancellationToken cancellationToken);
    Task<InventoryItem?> GetItemAsync(Guid stallId, Guid inventoryItemId, CancellationToken cancellationToken);
    Task<PagedResult<InventoryLedger>> GetLedgerAsync(InventoryLedgerListFilter filter, CancellationToken cancellationToken);
}
