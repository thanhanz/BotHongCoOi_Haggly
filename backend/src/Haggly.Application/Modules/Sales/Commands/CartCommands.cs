using Haggly.Application.Modules.Sales.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Sales.Commands;

public sealed record AddCartItemCommand(
    Guid BuyerId,
    Guid InventoryItemId,
    decimal Quantity,
    string? Notes) : IRequest<CartDto>;

public sealed record UpdateCartItemCommand(
    Guid BuyerId,
    Guid CartItemId,
    decimal Quantity,
    string? Notes) : IRequest<CartDto>;

public sealed record RemoveCartItemCommand(
    Guid BuyerId,
    Guid CartItemId) : IRequest<CartDto>;

public sealed record ClearCartCommand(Guid BuyerId) : IRequest<CartDto>;

public sealed record CheckoutCartCommand(Guid BuyerId) : IRequest<OrderDto>;
