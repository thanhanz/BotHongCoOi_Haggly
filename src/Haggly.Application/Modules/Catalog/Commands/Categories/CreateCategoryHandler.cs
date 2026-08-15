using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.Categories;
using Haggly.Application.Modules.Catalog.Dtos.Categories;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Application.Modules.Catalog.Validation.Categories;
using Haggly.Domain.Modules.Catalog;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Commands.Categories;

public sealed class CreateCategoryHandler(ICategoryCommandRepository repository)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        CategoryValidation.Validate(command);
        var name = command.Name.Trim();
        var slug = command.Slug.Trim().ToLowerInvariant();

        if (await repository.SlugExistsAsync(slug, null, cancellationToken))
            throw new CategoryConflictException("A category with this slug already exists.");

        if (command.ParentCategoryId is { } parentCategoryId)
        {
            var parentCategory = await repository.FindByIdAsync(parentCategoryId, cancellationToken);
            if (parentCategory is null || parentCategory.Status != CatalogStatus.ACTIVE)
                throw new CategoryNotFoundException("The parent category was not found.");
        }

        var category = new Category
        {
            Name = name,
            Slug = slug,
            Description = command.Description?.Trim(),
            ImageUrl = command.ImageUrl?.Trim(),
            ParentCategoryId = command.ParentCategoryId,
            DisplayOrder = command.DisplayOrder,
            Status = CatalogStatus.ACTIVE
        };

        await repository.AddAsync(category, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return CategoryDto.From(category);
    }
}
