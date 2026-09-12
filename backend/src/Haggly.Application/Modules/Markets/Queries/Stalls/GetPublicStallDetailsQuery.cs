using Haggly.Application.Modules.Markets.Dtos.Stalls;
using MediatR;

namespace Haggly.Application.Modules.Markets.Queries.Stalls;

public sealed record GetPublicStallDetailsQuery(Guid Id) : IRequest<PublicStallDetailsDto>;
