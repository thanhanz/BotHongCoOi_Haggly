using Haggly.Application.Modules.Sales.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Sales.Queries;

public sealed record GetOrderDetailsQuery(
    Guid OrderId,
    Guid BuyerId) : IRequest<OrderDto>;
