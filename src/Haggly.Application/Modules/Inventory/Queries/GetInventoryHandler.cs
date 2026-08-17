using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Inventory.Authorization;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed class GetInventoryHandler(IInventoryQuery query, IInventoryReferenceQuery references)
    : IRequestHandler<GetInventoryQuery, InventoryDto>
{
    public async Task<InventoryDto> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        if (request.StallId == Guid.Empty || request.ActorUserId == Guid.Empty)
            throw new InventoryValidationException("Valid stall and actor IDs are required.");

        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references, request.StallId, request.ActorUserId, cancellationToken);
        var inventory = await query.GetInventoryAsync(stall.Id, cancellationToken)
            ?? throw new InventoryNotFoundException("The stall inventory was not found.");
        return InventoryDto.From(inventory);
    }
}
