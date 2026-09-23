using Haggly.Application.Modules.Inventory.Dtos;

namespace Haggly.Application.Modules.Discovery.Dtos;

public sealed record StallSearchResult(
    Guid Id,
    string Code,
    string Name,
    string? LocationDescription,
    string? PhoneNumber,
    int AvailableProductCount,
    IReadOnlyCollection<ProductListingDto> ProductPreview);
