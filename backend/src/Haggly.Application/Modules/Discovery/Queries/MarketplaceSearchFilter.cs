namespace Haggly.Application.Modules.Discovery.Queries;

public sealed record MarketplaceSearchFilter(
    string NormalizedQuery,
    int StallPage,
    int StallPageSize,
    int ProductPage,
    int ProductPageSize);
