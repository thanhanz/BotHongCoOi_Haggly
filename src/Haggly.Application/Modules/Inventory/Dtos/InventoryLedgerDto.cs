using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Modules.Inventory.Dtos;

public sealed record InventoryLedgerDto(
    Guid Id,
    Guid InventoryItemId,
    Guid InventoryId,
    InventoryTransactionType TransactionType,
    decimal QuantityDelta,
    decimal QuantityBefore,
    decimal QuantityAfter,
    string ReferenceType,
    Guid? ReferenceId,
    string? Reason,
    Guid? PerformedBy,
    DateTimeOffset OccurredAt)
{
    public static InventoryLedgerDto From(InventoryLedger value)
        => new(value.Id, value.InventoryItemId, value.InventoryId, value.TransactionType,
            value.QuantityDelta, value.QuantityBefore, value.QuantityAfter, value.ReferenceType,
            value.ReferenceId, value.Reason, value.PerformedBy, value.OccurredAt);
}
