using Haggly.Domain.Modules.Markets;

namespace Haggly.Application.Modules.Markets.Dtos;

public sealed record MarketDto(
    Guid Id,
    string Code,
    string Name,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    TimeOnly? OpeningTime,
    TimeOnly? ClosingTime,
    MarketStatus Status)
{
    public static MarketDto From(Market market)
        => new(
            market.Id,
            market.Code,
            market.Name,
            market.Address,
            market.Latitude,
            market.Longitude,
            market.OpeningTime,
            market.ClosingTime,
            market.Status);
}
