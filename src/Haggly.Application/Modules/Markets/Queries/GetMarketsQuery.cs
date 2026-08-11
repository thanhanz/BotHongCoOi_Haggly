using Haggly.Application.Modules.Markets.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Markets.Queries;

public sealed record GetMarketsQuery : IRequest<IReadOnlyCollection<MarketDto>>;
