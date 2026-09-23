using Haggly.Application.Modules.Markets.Dtos.Stalls;
using Haggly.Domain.Modules.Markets;
using MediatR;

namespace Haggly.Application.Modules.Markets.Commands.Stalls;

public sealed record UpdateStallCommand(
    Guid Id,
    Guid MarketId,
    Guid VendorId,
    string Code,
    string Name,
    string? LocationDescription,
    string? PhoneNumber,
    StallStatus Status) : IRequest<StallDto>;
