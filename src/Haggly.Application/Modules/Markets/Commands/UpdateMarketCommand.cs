using Haggly.Application.Modules.Markets.Dtos;
using Haggly.Domain.Modules.Markets;
using MediatR;

namespace Haggly.Application.Modules.Markets.Commands;

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
