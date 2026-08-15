using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Application.Modules.Inventory.Validation;
using Haggly.Application.Modules.Inventory.Exceptions;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Handlers;

public sealed class GetInventoryLedgerHandler(
    IInventoryQuery query,
    IInventoryReferenceQuery references)
    : IRequestHandler<GetInventoryLedgerQuery, PagedResult<InventoryLedgerDto>>
{
    public async Task<PagedResult<InventoryLedgerDto>> Handle(
        GetInventoryLedgerQuery request,
        CancellationToken cancellationToken)
    {
        InventoryValidation.Validate(request);
        var stall = await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references,
            request.StallId,
            request.ownerId,
            cancellationToken);
        var filter = new InventoryLedgerListFilter(
            stall.Id,
            request.BusinessDate,
            request.ListingId,
            request.TransactionType,
            request.Page,
            request.PageSize);
        var result = await query.GetLedgerAsync(filter, cancellationToken);

        return new PagedResult<InventoryLedgerDto>(
            result.Items.Select(InventoryLedgerDto.From).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }
}
