using Haggly.Application.Modules.Inventory.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed record GetPreviousInventorySessionQuery(Guid StallId)
    : IRequest<InventorySessionDto>;
