using Haggly.Application.Modules.Catalog.Dtos.Products;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Queries.Products;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
