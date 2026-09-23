using Haggly.Domain.Modules.Markets;

namespace Haggly.Application.Modules.Markets.Dtos.Stalls;

public sealed record PublicStallDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? LocationDescription,
    string? PhoneNumber)
{
    public static PublicStallDetailsDto From(Stall stall)
        => new(stall.Id, stall.Code, stall.Name, stall.LocationDescription, stall.PhoneNumber);
}
