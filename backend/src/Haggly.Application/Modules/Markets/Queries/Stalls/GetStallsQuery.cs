using Haggly.Application.Modules.Markets.Dtos.Stalls;
using MediatR;

namespace Haggly.Application.Modules.Markets.Queries.Stalls;

public sealed record GetStallsQuery : IRequest<IReadOnlyCollection<StallDto>>;
