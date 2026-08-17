using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed record InventoryLedgerListFilter(
    Guid StallId,
    Guid? InventoryItemId,
    InventoryTransactionType? TransactionType,
    int Page,
    int PageSize);
