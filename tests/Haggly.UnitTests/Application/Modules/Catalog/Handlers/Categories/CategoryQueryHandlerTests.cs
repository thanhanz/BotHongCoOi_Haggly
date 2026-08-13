using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Application.Modules.Catalog.Handlers.Categories;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using Haggly.Domain.Modules.Catalog;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Handlers.Categories;

public sealed class CategoryQueryHandlerTests
{
    [Fact]
    public async Task HandleGetAll_WhenActiveCategoriesExist_ReturnsCategoryDtosInDisplayOrder()
    {
        var first = new Category { Name = "Fruit", Slug = "fruit", DisplayOrder = 1 };
        var second = new Category { Name = "Vegetables", Slug = "vegetables", DisplayOrder = 2 };
        var handler = new GetCategoriesHandler(new FakeCategoryQuery([first, second]));

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.Equal([first.Id, second.Id], result.Select(category => category.Id));
        Assert.All(result, category => Assert.Equal(CatalogStatus.ACTIVE, category.Status));
    }

    [Fact]
    public async Task HandleGetById_WhenCategoryExists_ReturnsCategoryDto()
    {
        var category = new Category { Name = "Fruit", Slug = "fruit" };
        var handler = new GetCategoryByIdHandler(new FakeCategoryQuery([], category));

        var result = await handler.Handle(new GetCategoryByIdQuery(category.Id), CancellationToken.None);

        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Fruit", result.Name);
        Assert.Equal("fruit", result.Slug);
    }

    [Fact]
    public async Task HandleGetById_WhenCategoryDoesNotExist_ThrowsNotFoundException()
    {
        var handler = new GetCategoryByIdHandler(new FakeCategoryQuery());

        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            handler.Handle(new GetCategoryByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeCategoryQuery(
        IReadOnlyCollection<Category>? categories = null,
        Category? category = null) : ICategoryQuery
    {
        public Task<IReadOnlyCollection<Category>> GetAllActiveAsync(CancellationToken cancellationToken)
            => Task.FromResult(categories ?? []);

        public Task<Category?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(category?.Id == id && category.DeletedAt is null
                && category.Status == CatalogStatus.ACTIVE
                    ? category
                    : null);
    }
}
