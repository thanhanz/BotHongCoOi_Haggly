using Haggly.Application.Modules.Sales.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed record CancelOrderCommand(
    Guid OrderId,
    Guid BuyerId,
    string Reason) : IRequest<OrderDto>;
