using Haggly.Application.Modules.Sales.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed record CreateOrderCommand(
    Guid BuyerId,
    IReadOnlyCollection<CreateOrderLine> Items) : IRequest<OrderDto>;

public sealed record CreateOrderLine(
    Guid InventoryItemId,
    decimal Quantity,
    string? Notes);
