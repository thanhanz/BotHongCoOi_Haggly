using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Inventory.Authorization;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed class GetInventoryItemHandler(IInventoryQuery query, IInventoryReferenceQuery references)
    : IRequestHandler<GetInventoryItemQuery, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(GetInventoryItemQuery request, CancellationToken cancellationToken)
    {
        if (request.StallId == Guid.Empty || request.InventoryItemId == Guid.Empty || request.ActorUserId == Guid.Empty)
            throw new InventoryValidationException("Valid stall, inventory-item, and actor IDs are required.");

        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references, request.StallId, request.ActorUserId, cancellationToken);
        var item = await query.GetItemAsync(stall.Id, request.InventoryItemId, cancellationToken)
            ?? throw new InventoryNotFoundException("The inventory item was not found.");
        return InventoryItemDto.From(item);
    }
}
