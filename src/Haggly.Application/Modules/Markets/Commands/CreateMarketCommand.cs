using Haggly.Application.Modules.Markets.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Markets.Commands;

public sealed record CreateMarketCommand(
    string Code,
    string Name,
    string Address,
    decimal? Latitude = null,
    decimal? Longitude = null,
    TimeOnly? OpeningTime = null,
    TimeOnly? ClosingTime = null) : IRequest<MarketDto>;
