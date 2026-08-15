using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Application.Modules.Inventory.Validation;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Handlers;

public sealed class GetPreviousInventorySessionHandler(
    IInventoryQuery query,
    IInventoryReferenceQuery references,
    IBusinessClock businessClock)
    : IRequestHandler<GetPreviousInventorySessionQuery, InventorySessionDto>
{
    public async Task<InventorySessionDto> Handle(
        GetPreviousInventorySessionQuery request,
        CancellationToken cancellationToken)
    {
        InventoryValidation.Validate(request);
        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references,
            request.StallId,
            request.ActorUserId,
            cancellationToken);
        var session = await query.GetPreviousSessionAsync(
                stall.Id,
                businessClock.GetBusinessDate(),
                cancellationToken)
            ?? throw new InventoryNotFoundException("The previous inventory session was not found.");

        if (session.StallId != stall.Id)
        {
            throw new InventoryNotFoundException("The previous inventory session was not found.");
        }

        return InventorySessionDto.From(session);
    }
}
