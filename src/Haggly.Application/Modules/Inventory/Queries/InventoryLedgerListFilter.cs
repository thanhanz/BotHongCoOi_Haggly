using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed record InventoryLedgerListFilter(
    Guid StallId,
    DateOnly? BusinessDate,
    Guid? ListingId,
    InventoryTransactionType? TransactionType,
    int Page,
    int PageSize);
