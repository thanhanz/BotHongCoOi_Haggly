using Haggly.Application.Abstractions.Discovery;
using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Domain.Modules.Discovery;
using MediatR;

namespace Haggly.Application.Modules.Discovery.Queries;

public sealed class SearchCommonDishesHandler(IDiscoveryQuery query)
    : IRequestHandler<SearchCommonDishesQuery, CommonDishSearchResult>
{
    public async Task<CommonDishSearchResult> Handle(
        SearchCommonDishesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Length > 200)
        {
            throw new DiscoveryValidationException(
                "Query is required and must not exceed 200 characters.");
        }

        var normalizedQuery = VietnameseNameNormalizer.Normalize(request.Query);
        var dish = await query.FindDishAsync(normalizedQuery, cancellationToken);

        return new CommonDishSearchResult(dish is null ? [] : [dish]);
    }
}
