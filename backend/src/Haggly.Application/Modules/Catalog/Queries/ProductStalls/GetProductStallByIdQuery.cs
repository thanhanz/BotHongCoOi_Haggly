using Haggly.Application.Modules.Catalog.Dtos.ProductStalls;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Queries.ProductStalls;

public sealed record GetProductStallByIdQuery(Guid StallId, Guid Id) : IRequest<ProductStallDto>;
