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
        if (c.MinimumOrderQuantity is <= 0 || c.CurrentUnitPrice is < 0 || c.ExpectedVersion < 0)
            throw new ProductStallValidationException("Quantity and price are invalid.");
        if (value.Version != c.ExpectedVersion)
            throw new ProductStallConflictException("The stall product was changed by another request. Refresh and retry.");
        
        value.UpdateConfiguration(c.DisplayName, c.SellingUnit, c.MinimumOrderQuantity,
            c.CurrentUnitPrice, c.IsNegotiable, c.IsActive);
        
        await repository.SaveChangesAsync(ct); 
        return ProductStallDto.From(value);
    }
}
