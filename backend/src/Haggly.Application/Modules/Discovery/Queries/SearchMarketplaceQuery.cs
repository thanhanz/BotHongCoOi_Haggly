using Haggly.Application.Modules.Discovery.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Discovery.Queries;

public sealed record SearchMarketplaceQuery(
    string Query,
    int StallPage,
    int StallPageSize,
    int ProductPage,
    int ProductPageSize) : IRequest<MarketplaceSearchResult>;
