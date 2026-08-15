using Haggly.Application.Modules.Inventory.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Commands;

public sealed record OpenInventorySessionCommand(
    Guid StallId,
    Guid ActorUserId,
    string? Notes,
    IReadOnlyCollection<InventoryListingInput> Listings) : IRequest<InventorySessionDto>;
