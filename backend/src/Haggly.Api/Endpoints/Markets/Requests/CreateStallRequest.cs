namespace Haggly.Api.Endpoints.Markets.Requests;

public sealed record CreateStallRequest(
    Guid MarketId,
    Guid VendorId,
    string Code,
    string Name,
    string? LocationDescription = null,
    string? PhoneNumber = null);
