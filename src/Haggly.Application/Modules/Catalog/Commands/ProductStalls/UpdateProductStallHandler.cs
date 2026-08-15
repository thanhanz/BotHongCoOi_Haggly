using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.ProductStalls;
using Haggly.Application.Modules.Catalog.Dtos.ProductStalls;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Commands.ProductStalls;

public sealed class UpdateProductStallHandler(IProductStallCommandRepository repository)
    : IRequestHandler<UpdateProductStallCommand, ProductStallDto>
{
    public async Task<ProductStallDto> Handle(UpdateProductStallCommand c, CancellationToken ct)
    {
        if (c.StallId == Guid.Empty || c.Id == Guid.Empty || c.ActorUserId == Guid.Empty)
            throw new ProductStallValidationException("Valid stall, product-stall, and actor IDs are required.");
        
        var stall = await repository.FindActiveStallAsync(c.StallId, ct)
            ?? throw new ProductStallNotFoundException("The stall was not found.");
        if (stall.VendorId != c.ActorUserId)
            throw new ProductStallForbiddenException("Only the stall owner can update products in this stall.");
        
        var value = await repository.FindActiveAsync(c.Id, ct)
            ?? throw new ProductStallNotFoundException("The product was not found in this stall.");
        if (value.StallId != c.StallId)
            throw new ProductStallNotFoundException("The product was not found in this stall.");
        if (c.SellingUnit is not null && !Enum.IsDefined(c.SellingUnit.Value))
            throw new ProductStallValidationException("A valid product unit is required.");
        if (c.MinimumOrderQuantity is <= 0 || c.DefaultUnitPrice is < 0)
            throw new ProductStallValidationException("Quantity and price are invalid.");
        
        if (c.DisplayName is not null) value.DisplayName = c.DisplayName.Trim();
        if (c.SellingUnit is not null) value.SellingUnit = c.SellingUnit.Value;
        if (c.MinimumOrderQuantity is not null) value.MinimumOrderQuantity = c.MinimumOrderQuantity.Value;
        if (c.DefaultUnitPrice is not null) value.DefaultUnitPrice = c.DefaultUnitPrice.Value;
        if (c.IsNegotiable is not null) value.IsNegotiable = c.IsNegotiable.Value;
        if (c.IsActive is not null) value.IsActive = c.IsActive.Value;
        
        await repository.SaveChangesAsync(ct); 
        return ProductStallDto.From(value);
    }
}
