using Haggly.Application.Modules.Markets.Dtos.Stalls;
using MediatR;

namespace Haggly.Application.Modules.Markets.Commands.Stalls;

public sealed record CreateStallCommand(
    Guid MarketId,
    Guid VendorId,
    Guid ActorUserId,
    string Code,
    string Name,
    string? LocationDescription = null,
    string? PhoneNumber = null) : IRequest<StallDto>;
