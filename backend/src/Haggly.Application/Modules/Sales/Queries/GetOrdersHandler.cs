using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Validation;
using MediatR;

namespace Haggly.Application.Modules.Sales.Queries;

public sealed class GetOrdersHandler(IOrderQuery query)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(
        GetOrdersQuery request,
        CancellationToken cancellationToken)
    {
        OrderValidation.Validate(request);
        var result = await query.GetPageAsync(
            request.BuyerId,
            request.Page,
            request.PageSize,
            cancellationToken);
        return new PagedResult<OrderDto>(
            result.Items.Select(OrderDto.From).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }
}
