using Haggly.Application.Modules.Sales.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Sales.Queries;

public sealed record GetPosSaleDetailsQuery(
    Guid StallId,
    Guid PosSaleId,
    Guid ActorUserId) : IRequest<PosSaleDto>;
