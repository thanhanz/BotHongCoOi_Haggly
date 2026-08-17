using Haggly.Domain.Modules.Inventory;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.Application.Modules.Inventory.Dtos;

public sealed record InventoryDto(Guid Id, Guid StallId, IReadOnlyCollection<InventoryItemDto> Items)
{
    public static InventoryDto From(DomainInventory value)
        => new(value.Id, value.StallId, value.Items.Select(InventoryItemDto.From).ToArray());
}
