using Haggly.Application.Abstractions.Discovery;
using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Domain.Modules.Discovery;
using MediatR;

namespace Haggly.Application.Modules.Discovery.Queries;

public sealed class FindCanonicalIngredientsHandler(IDiscoveryQuery query)
    : IRequestHandler<FindCanonicalIngredientsQuery, IReadOnlyList<CanonicalIngredientResult>>
{
    public Task<IReadOnlyList<CanonicalIngredientResult>> Handle(
        FindCanonicalIngredientsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Length > 200)
        {
            throw new DiscoveryValidationException(
                "Query is required and must not exceed 200 characters.");
        }

        var normalizedQuery = VietnameseNameNormalizer.Normalize(request.Query);
        return query.FindCanonicalIngredientsAsync(normalizedQuery, cancellationToken);
    }
}
