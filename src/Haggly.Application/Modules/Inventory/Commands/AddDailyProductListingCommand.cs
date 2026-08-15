using Haggly.Application.Modules.Inventory.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Commands;

public sealed record AddDailyProductListingCommand(
    Guid StallId,
    Guid ActorUserId,
    InventoryListingInput Listing) : IRequest<DailyProductListingDto>;
