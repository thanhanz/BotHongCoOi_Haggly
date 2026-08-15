using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.ProductStalls;
using Haggly.Application.Modules.Catalog.Dtos.ProductStalls;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using Haggly.Domain.Modules.Catalog;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Commands.ProductStalls;

public sealed class CreateProductStallHandler(IProductStallCommandRepository repository)
    : IRequestHandler<CreateProductStallCommand, ProductStallDto>
{
    public async Task<ProductStallDto> Handle(CreateProductStallCommand c, CancellationToken ct)
    {
        Validate(c.StallId, c.ProductId, c.ActorUserId, c.SellingUnit, c.MinimumOrderQuantity, c.DefaultUnitPrice);
        
        var stall = await repository.FindActiveStallAsync(c.StallId, ct)
            ?? throw new ProductStallNotFoundException("The stall was not found.");
        
        if (stall.VendorId != c.ActorUserId)
            throw new ProductStallForbiddenException("Only the stall owner can add products to this stall.");
        
        if (await repository.FindActiveProductAsync(c.ProductId, ct) is null)
            throw new ProductStallNotFoundException("The product was not found.");
        
        if (await repository.ExistsAsync(c.StallId, c.ProductId, ct))
            throw new ProductStallConflictException("This product is already attached to the stall.");
        
        var value = new ProductStall { 
                          StallId = c.StallId, 
                          ProductId = c.ProductId,
                          DisplayName = c.DisplayName?.Trim(), 
                          SellingUnit = c.SellingUnit,
                          MinimumOrderQuantity = c.MinimumOrderQuantity, 
                          DefaultUnitPrice = c.DefaultUnitPrice,
                          IsNegotiable = c.IsNegotiable, 
                          IsActive = true };
        
        await repository.AddAsync(value, ct); 
        await repository.SaveChangesAsync(ct);
        return ProductStallDto.From(value);
    }

    internal static void Validate(Guid stallId, Guid productId, Guid actor, ProductUnit unit, decimal min, decimal price)
    {
        if (stallId == Guid.Empty || productId == Guid.Empty || actor == Guid.Empty)
            throw new ProductStallValidationException("Valid stall, product, and actor IDs are required.");
        
        if (!Enum.IsDefined(unit) || min <= 0 || price < 0)
            throw new ProductStallValidationException("Selling unit, minimum quantity, and price are invalid.");
    }
}
