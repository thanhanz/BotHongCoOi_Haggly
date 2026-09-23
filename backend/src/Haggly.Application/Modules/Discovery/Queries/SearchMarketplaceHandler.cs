using Haggly.Application.Abstractions.Discovery;
using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Domain.Modules.Discovery;
using MediatR;

namespace Haggly.Application.Modules.Discovery.Queries;

public sealed class SearchMarketplaceHandler(IMarketplaceSearchReader reader)
    : IRequestHandler<SearchMarketplaceQuery, MarketplaceSearchResult>
{
    public Task<MarketplaceSearchResult> Handle(
        SearchMarketplaceQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Length > 100)
            throw new DiscoveryValidationException("Query must not exceed 100 characters.");

        var normalizedQuery = VietnameseNameNormalizer.Normalize(request.Query);
        if (normalizedQuery.Length is < 2 or > 100)
            throw new DiscoveryValidationException("Query must contain between 2 and 100 searchable characters.");

        if (request.StallPage < 1 || request.ProductPage < 1)
            throw new DiscoveryValidationException("Page must be at least 1.");

        if (request.StallPageSize is < 1 or > 20)
            throw new DiscoveryValidationException("Stall page size must be between 1 and 20.");

        if (request.ProductPageSize is < 1 or > 100)
            throw new DiscoveryValidationException("Product page size must be between 1 and 100.");

        return reader.SearchAsync(
            new MarketplaceSearchFilter(
                normalizedQuery,
                request.StallPage,
                request.StallPageSize,
                request.ProductPage,
                request.ProductPageSize),
            cancellationToken);
    }
}
