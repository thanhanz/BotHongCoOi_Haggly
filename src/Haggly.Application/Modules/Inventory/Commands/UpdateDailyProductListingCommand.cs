using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Domain.Modules.Inventory;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Commands;

public sealed record UpdateDailyProductListingCommand(
    Guid StallId,
    Guid ListingId,
    Guid ActorUserId,
    decimal? PublicUnitPrice,
    DailyListingStatus? Status,
    long ExpectedVersion) : IRequest<DailyProductListingDto>;
