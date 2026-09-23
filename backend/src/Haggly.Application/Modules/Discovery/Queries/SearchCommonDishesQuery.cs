using Haggly.Application.Modules.Discovery.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Discovery.Queries;

public sealed record SearchCommonDishesQuery(string Query)
    : IRequest<CommonDishSearchResult>;
