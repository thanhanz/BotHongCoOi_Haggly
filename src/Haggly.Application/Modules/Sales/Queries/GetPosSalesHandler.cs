using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Authorization;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using MediatR;

namespace Haggly.Application.Modules.Sales.Queries;

public sealed class GetPosSalesHandler(
    IInventoryReferenceQuery references,
    IPosSaleQuery query)
    : IRequestHandler<GetPosSalesQuery, PagedResult<PosSaleDto>>
{
    public async Task<PagedResult<PosSaleDto>> Handle(
        GetPosSalesQuery request,
        CancellationToken cancellationToken)
    {
        await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references,
            request.StallId,
            request.ActorUserId,
            cancellationToken);

        if (request.Page < 1 || request.PageSize is < 1 or > 100)
        {
            throw new PosSaleValidationException("Valid page and page size are required.");
        }

        var result = await query.GetPageAsync(
            request.StallId,
            request.Page,
            request.PageSize,
            cancellationToken);
        return new PagedResult<PosSaleDto>(
            result.Items.Select(PosSaleDto.From).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }
}
