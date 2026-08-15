using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Application.Modules.Inventory.Validation;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Handlers;

public sealed class GetCurrentInventorySessionHandler(
    IInventoryQuery query,
    IInventoryReferenceQuery references,
    IBusinessClock businessClock)
    : IRequestHandler<GetCurrentInventorySessionQuery, InventorySessionDto>
{
    public async Task<InventorySessionDto> Handle(
        GetCurrentInventorySessionQuery request,
        CancellationToken cancellationToken)
    {
        InventoryValidation.Validate(request);
        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references,
            request.StallId,
            request.ActorUserId,
            cancellationToken);
        var session = await query.GetCurrentSessionAsync(
                stall.Id,
                businessClock.GetBusinessDate(),
                cancellationToken)
            ?? throw new InventoryNotFoundException("The current inventory session was not found.");

        if (session.StallId != stall.Id)
        {
            throw new InventoryNotFoundException("The current inventory session was not found.");
        }

        return InventorySessionDto.From(session);
    }
}
