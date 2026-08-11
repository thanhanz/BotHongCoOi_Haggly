using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Dtos;
using Haggly.Application.Modules.Markets.Queries;
using MediatR;

namespace Haggly.Application.Modules.Markets.Handlers;

public sealed class GetMarketsHandler(IMarketQuery query)
    : IRequestHandler<GetMarketsQuery, IReadOnlyCollection<MarketDto>>
{
    public async Task<IReadOnlyCollection<MarketDto>> Handle(
        GetMarketsQuery request,
        CancellationToken cancellationToken)
    {
        var markets = await query.GetAllAsync(cancellationToken);

        return markets
            .Select(MarketDto.From)
            .ToArray();
    }
}
