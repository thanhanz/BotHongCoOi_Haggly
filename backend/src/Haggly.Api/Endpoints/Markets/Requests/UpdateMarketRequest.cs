using Haggly.Domain.Modules.Markets;

namespace Haggly.Api.Endpoints.Markets.Requests;

public sealed record UpdateMarketRequest(
    string Code,
    string Name,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    TimeOnly? OpeningTime,
    TimeOnly? ClosingTime,
    MarketStatus Status);
