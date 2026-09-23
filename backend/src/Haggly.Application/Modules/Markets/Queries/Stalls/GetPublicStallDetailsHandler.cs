using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Dtos.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Domain.Modules.Markets;
using MediatR;

namespace Haggly.Application.Modules.Markets.Queries.Stalls;

public sealed class GetPublicStallDetailsHandler(IStallQuery query)
    : IRequestHandler<GetPublicStallDetailsQuery, PublicStallDetailsDto>
{
    public async Task<PublicStallDetailsDto> Handle(
        GetPublicStallDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
            throw new StallValidationException("A valid stall ID is required.");

        var stall = await query.GetByIdAsync(request.Id, cancellationToken);

        if (stall is null || stall.Status != StallStatus.ACTIVE)
            throw new StallNotFoundException("The stall was not found.");

        return PublicStallDetailsDto.From(stall);
    }
}
