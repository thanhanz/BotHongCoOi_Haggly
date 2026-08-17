using Haggly.Application.Modules.Catalog.Dtos.ProductStalls;
using Haggly.Domain.Modules.Catalog;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Commands.ProductStalls;

public sealed record UpdateProductStallCommand(
    Guid StallId, 
    Guid Id, 
    Guid ActorUserId, 
    string? DisplayName, 
    ProductUnit? SellingUnit,
    decimal? MinimumOrderQuantity, 
    decimal? CurrentUnitPrice,
    bool? IsNegotiable, 
    bool? IsActive,
    long ExpectedVersion) : IRequest<ProductStallDto>;
