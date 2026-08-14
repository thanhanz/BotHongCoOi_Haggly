using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.Products;
using Haggly.Application.Modules.Catalog.Dtos.Products;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Validation.Products;
using Haggly.Domain.Modules.Catalog;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Handlers.Products;

public sealed class CreateProductHandler(IProductCommandRepository repository)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        ProductValidation.Validate(command);
        var name = command.Name.Trim();

        if (await repository.FindActiveCategoryByIdAsync(command.CategoryId, cancellationToken) is null)
            throw new ProductNotFoundException("The product category was not found.");

        if (await repository.NameExistsAsync(command.CategoryId, name, null, cancellationToken))
            throw new ProductConflictException("A product with this name already exists in the category.");

        var product = new Product
        {
            CategoryId = command.CategoryId,
            Name = name,
            Description = command.Description?.Trim(),
            DefaultUnit = command.DefaultUnit,
            ImageUrl = command.ImageUrl?.Trim(),
            Status = CatalogStatus.ACTIVE
        };

        await repository.AddAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ProductDto.From(product);
    }
}
