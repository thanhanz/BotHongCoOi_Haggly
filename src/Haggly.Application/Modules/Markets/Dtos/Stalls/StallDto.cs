using Haggly.Domain.Modules.Markets;

namespace Haggly.Application.Modules.Markets.Dtos.Stalls;

public sealed record StallDto(
    Guid Id,
    Guid MarketId,
    Guid VendorId,
    string Code,
    string Name,
    string? LocationDescription,
    string? PhoneNumber,
    StallStatus Status)
{
    public static StallDto From(Stall stall)
        => new(
            stall.Id,
            stall.MarketId,
            stall.VendorId,
            stall.Code,
            stall.Name,
            stall.LocationDescription,
            stall.PhoneNumber,
            stall.Status);
}
