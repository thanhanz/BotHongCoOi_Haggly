using Haggly.Application.Modules.Discovery.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Discovery.Queries;

public sealed record FindCanonicalIngredientsQuery(string Query)
    : IRequest<IReadOnlyList<CanonicalIngredientResult>>;
