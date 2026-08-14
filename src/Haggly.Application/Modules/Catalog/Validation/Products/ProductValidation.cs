using Haggly.Application.Modules.Catalog.Commands.Products;
using Haggly.Application.Modules.Catalog.Exceptions.Products;

namespace Haggly.Application.Modules.Catalog.Validation.Products;

internal static class ProductValidation
{
    private const int NameMaximumLength = 200;

    public static void Validate(CreateProductCommand command)
    {
        if (command.CategoryId == Guid.Empty)
            throw new ProductValidationException("A valid category ID is required.");

        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > NameMaximumLength)
            throw new ProductValidationException(
                $"Product name is required and must not exceed {NameMaximumLength} characters.");

        if (!Enum.IsDefined(command.DefaultUnit))
            throw new ProductValidationException("A valid product unit is required.");
    }
}
