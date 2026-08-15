using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Abstractions.Inventory;

public interface IInventoryQuery
{
    Task<InventorySession?> GetCurrentSessionAsync(
        Guid stallId,
        DateOnly businessDate,
        CancellationToken cancellationToken);

    Task<InventorySession?> GetPreviousSessionAsync(
        Guid stallId,
        DateOnly businessDate,
        CancellationToken cancellationToken);

    Task<PagedResult<InventoryLedger>> GetLedgerAsync(
        InventoryLedgerListFilter filter,
        CancellationToken cancellationToken);
}
