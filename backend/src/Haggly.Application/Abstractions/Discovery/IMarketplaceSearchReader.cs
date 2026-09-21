using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Application.Modules.Discovery.Queries;

namespace Haggly.Application.Abstractions.Discovery;

public interface IMarketplaceSearchReader
{
    Task<MarketplaceSearchResult> SearchAsync(
        MarketplaceSearchFilter filter,
        CancellationToken cancellationToken);
}
