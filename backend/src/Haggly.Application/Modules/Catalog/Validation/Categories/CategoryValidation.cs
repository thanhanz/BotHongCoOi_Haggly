using Haggly.Application.Modules.Catalog.Commands.Categories;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;

namespace Haggly.Application.Modules.Catalog.Validation.Categories;

internal static class CategoryValidation
{
    private const int NameMaximumLength = 200;
    private const int SlugMaximumLength = 200;

    public static void Validate(CreateCategoryCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > NameMaximumLength)
            throw new CategoryValidationException(
                $"Category name is required and must not exceed {NameMaximumLength} characters.");

        if (string.IsNullOrWhiteSpace(command.Slug) || command.Slug.Trim().Length > SlugMaximumLength)
            throw new CategoryValidationException(
                $"Category slug is required and must not exceed {SlugMaximumLength} characters.");

        if (command.DisplayOrder < 0)
            throw new CategoryValidationException("Category display order cannot be negative.");
    }
}
