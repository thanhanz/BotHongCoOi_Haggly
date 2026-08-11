using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Dtos;
using Haggly.Application.Modules.Markets.Exceptions;
using Haggly.Application.Modules.Markets.Queries;
using MediatR;

namespace Haggly.Application.Modules.Markets.Handlers;

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
