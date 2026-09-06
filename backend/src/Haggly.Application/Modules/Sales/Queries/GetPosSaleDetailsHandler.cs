using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Modules.Inventory.Authorization;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using MediatR;

namespace Haggly.Application.Modules.Sales.Queries;

public sealed class GetPosSaleDetailsHandler(
    IInventoryReferenceQuery references,
    IPosSaleQuery query)
    : IRequestHandler<GetPosSaleDetailsQuery, PosSaleDto>
{
    public async Task<PosSaleDto> Handle(
        GetPosSaleDetailsQuery request,
        CancellationToken cancellationToken)
    {
        await InventoryStallAccess.RequireOwnedActiveStallAsync(
            references,
            request.StallId,
            request.ActorUserId,
            cancellationToken);

        var sale = await query.GetByIdWithItemsAsync(
            request.StallId,
            request.PosSaleId,
            cancellationToken);

        return sale is null
            ? throw new PosSaleNotFoundException("The POS sale was not found in this stall.")
            : PosSaleDto.From(sale);
    }
}
