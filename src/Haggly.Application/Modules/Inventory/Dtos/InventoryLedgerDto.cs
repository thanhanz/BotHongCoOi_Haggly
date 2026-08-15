using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Modules.Inventory.Dtos;

public sealed record InventoryLedgerDto(
    Guid Id,
    Guid DailyProductListingId,
    Guid InventorySessionId,
    InventoryTransactionType TransactionType,
    decimal QuantityDelta,
    decimal QuantityBefore,
    decimal QuantityAfter,
    decimal? UnitPriceBefore,
    decimal? UnitPriceAfter,
    string ReferenceType,
    Guid? ReferenceId,
    string? Reason,
    Guid? PerformedBy,
    DateTimeOffset OccurredAt)
{
    public static InventoryLedgerDto From(InventoryLedger value)
        => new(
            value.Id,
            value.DailyProductListingId,
            value.InventorySessionId,
            value.TransactionType,
            value.QuantityDelta,
            value.QuantityBefore,
            value.QuantityAfter,
            value.UnitPriceBefore,
            value.UnitPriceAfter,
            value.ReferenceType,
            value.ReferenceId,
            value.Reason,
            value.PerformedBy,
            value.OccurredAt);
}
