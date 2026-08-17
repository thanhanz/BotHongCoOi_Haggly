using Haggly.Application.Modules.Inventory.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed record GetInventoryQuery(Guid StallId, Guid ActorUserId) : IRequest<InventoryDto>;
