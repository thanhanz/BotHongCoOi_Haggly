using Haggly.Application.Modules.Catalog.Dtos.ProductStalls;
using Haggly.Domain.Modules.Catalog;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Commands.ProductStalls;

public sealed record CreateProductStallCommand(
    Guid StallId, 
    Guid ProductId, 
    Guid ActorUserId, 
    string? DisplayName,
    ProductUnit SellingUnit, 
    decimal MinimumOrderQuantity, 
    decimal DefaultUnitPrice,
    bool IsNegotiable) : IRequest<ProductStallDto>;
