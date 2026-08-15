using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Modules.Inventory.Dtos;

public sealed record InventorySessionDto(
    Guid Id,
    Guid StallId,
    DateOnly BusinessDate,
    InventorySessionStatus Status,
    DateTimeOffset OpenedAt,
    Guid OpenedBy,
    DateTimeOffset? ClosedAt,
    Guid? ClosedBy,
    string? Notes,
    IReadOnlyCollection<DailyProductListingDto> Listings)
{
    public static InventorySessionDto From(InventorySession value)
        => new(
            value.Id,
            value.StallId,
            value.BusinessDate,
            value.Status,
            value.OpenedAt,
            value.OpenedBy,
            value.ClosedAt,
            value.ClosedBy,
            value.Notes,
            value.DailyProductListings.Select(DailyProductListingDto.From).ToArray());
}
