using Haggly.Application.Modules.Inventory.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed record GetInventoryItemQuery(Guid StallId, Guid InventoryItemId, Guid ActorUserId)
    : IRequest<InventoryItemDto>;
