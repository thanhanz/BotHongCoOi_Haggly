using Haggly.Application.Modules.Catalog.Dtos.Products;
using Haggly.Domain.Modules.Catalog;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Commands.Products;

public sealed record CreateProductCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    ProductUnit DefaultUnit,
    string? ImageUrl) : IRequest<ProductDto>;
