using Haggly.Domain.Modules.Markets;

namespace Haggly.Api.Endpoints.Markets.Requests;

public sealed record UpdateStallRequest(
    Guid MarketId,
    Guid VendorId,
    string Code,
    string Name,
    string? LocationDescription,
    string? PhoneNumber,
    StallStatus Status);
