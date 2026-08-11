using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Dtos.Stalls;
using Haggly.Application.Modules.Markets.Queries.Stalls;
using MediatR;

namespace Haggly.Application.Modules.Markets.Handlers.Stalls;

public sealed class GetStallsHandler(IStallQuery query)
    : IRequestHandler<GetStallsQuery, IReadOnlyCollection<StallDto>>
{
    public async Task<IReadOnlyCollection<StallDto>> Handle(
        GetStallsQuery request,
        CancellationToken cancellationToken)
    {
        var stalls = await query.GetAllAsync(cancellationToken);

        return stalls
            .Select(StallDto.From)
            .ToArray();
    }
}
