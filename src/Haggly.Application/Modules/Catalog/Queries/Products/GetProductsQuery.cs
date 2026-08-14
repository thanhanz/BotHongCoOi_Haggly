using Haggly.Application.Modules.Catalog.Dtos.Products;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Queries.Products;

public sealed record GetProductsQuery(Guid? CategoryId = null) : IRequest<IReadOnlyCollection<ProductDto>>;
