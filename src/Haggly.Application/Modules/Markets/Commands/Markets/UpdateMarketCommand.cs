using Haggly.Application.Modules.Markets.Dtos.Markets;
using Haggly.Domain.Modules.Markets;
using MediatR;

namespace Haggly.Application.Modules.Markets.Commands.Markets;

public sealed record UpdateMarketCommand(
    Guid Id,
    string Code,
    string Name,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    TimeOnly? OpeningTime,
    TimeOnly? ClosingTime,
    MarketStatus Status) : IRequest<MarketDto>;
