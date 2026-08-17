using Haggly.Application.Common;
using Haggly.Application.Modules.Sales.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Sales.Queries;

public sealed record GetOrdersQuery(
    Guid BuyerId,
    int Page,
    int PageSize) : IRequest<PagedResult<OrderDto>>;
