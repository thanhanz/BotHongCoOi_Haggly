namespace Haggly.Api.Endpoints.Markets.Requests;

public sealed record CreateMarketRequest(
    string Code,
    string Name,
    string Address,
    decimal? Latitude = null,
    decimal? Longitude = null,
    TimeOnly? OpeningTime = null,
    TimeOnly? ClosingTime = null);
