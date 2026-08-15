using Haggly.Application.Modules.Inventory.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Commands;

public sealed record CloseInventorySessionCommand(
    Guid StallId,
    Guid ActorUserId) : IRequest<InventorySessionDto>;
