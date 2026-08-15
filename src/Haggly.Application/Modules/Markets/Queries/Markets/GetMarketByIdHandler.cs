using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Dtos.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Application.Modules.Markets.Queries.Markets;
using MediatR;

namespace Haggly.Application.Modules.Markets.Queries.Markets;

public sealed class GetMarketByIdHandler(IMarketQuery query)
    : IRequestHandler<GetMarketByIdQuery, MarketDto>
{
    public async Task<MarketDto> Handle(
        GetMarketByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
            throw new MarketValidationException("A valid market ID is required.");

        var market = await query.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new MarketNotFoundException("The market was not found.");

        return MarketDto.From(market);
    }
}
