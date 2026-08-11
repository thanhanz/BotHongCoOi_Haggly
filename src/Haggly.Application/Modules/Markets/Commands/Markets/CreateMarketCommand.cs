using Haggly.Application.Modules.Markets.Dtos.Markets;
using MediatR;

namespace Haggly.Application.Modules.Markets.Commands.Markets;

public sealed record CreateMarketCommand(
    string Code,
    string Name,
    string Address,
    decimal? Latitude = null,
    decimal? Longitude = null,
    TimeOnly? OpeningTime = null,
    TimeOnly? ClosingTime = null) : IRequest<MarketDto>;
