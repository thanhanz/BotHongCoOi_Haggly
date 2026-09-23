using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Dtos.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Application.Modules.Markets.Queries.Stalls;
using MediatR;

namespace Haggly.Application.Modules.Markets.Queries.Stalls;

public sealed class GetStallByIdHandler(IStallQuery query)
    : IRequestHandler<GetStallByIdQuery, StallDto>
{
    public async Task<StallDto> Handle(
        GetStallByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
            throw new StallValidationException("A valid stall ID is required.");

        var stall = await query.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new StallNotFoundException("The stall was not found.");

        return StallDto.From(stall);
    }
}
