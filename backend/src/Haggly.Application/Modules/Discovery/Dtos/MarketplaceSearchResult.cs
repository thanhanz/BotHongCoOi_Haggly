using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;

namespace Haggly.Application.Modules.Discovery.Dtos;

public sealed record MarketplaceSearchResult(
    PagedResult<StallSearchResult> Stalls,
    PagedResult<ProductListingDto> Products);
