using Haggly.Application.Modules.Markets.Dtos.Markets;
using MediatR;

namespace Haggly.Application.Modules.Markets.Queries.Markets;

public sealed record GetMarketsQuery : IRequest<IReadOnlyCollection<MarketDto>>;
