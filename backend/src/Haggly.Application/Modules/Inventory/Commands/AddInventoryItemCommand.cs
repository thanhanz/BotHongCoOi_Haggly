using Haggly.Application.Modules.Inventory.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Commands;

public sealed record AddInventoryItemCommand(
    Guid StallId,
    Guid ActorUserId,
    Guid ProductStallId,
    decimal CurrentQuantity) : IRequest<InventoryItemDto>;
