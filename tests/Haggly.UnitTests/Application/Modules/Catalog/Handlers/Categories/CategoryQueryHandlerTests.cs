using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Application.Modules.Catalog.Handlers.Categories;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using Haggly.Domain.Modules.Catalog;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Handlers.Categories;

public sealed class CategoryQueryHandlerTests
{
    [Fact]
    public async Task HandleGetAll_WhenActiveCategoriesExist_ReturnsPagedCategoryDtosInDisplayOrder()
    {
        var first = new Category { Name = "Fruit", Slug = "fruit", DisplayOrder = 1 };
        var second = new Category { Name = "Vegetables", Slug = "vegetables", DisplayOrder = 2 };
        var handler = new GetCategoriesHandler(new FakeCategoryQuery([first, second]));

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.Equal([first.Id, second.Id], result.Items.Select(category => category.Id));
        Assert.All(result.Items, category => Assert.Equal(CatalogStatus.ACTIVE, category.Status));
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task HandleGetAll_WhenPagingIsProvided_PassesItToTheQuery()
    {
        var query = new FakeCategoryQuery();
        var handler = new GetCategoriesHandler(query);

        await handler.Handle(new GetCategoriesQuery(2, 50), CancellationToken.None);

        Assert.Equal(2, query.LastFilter!.Page);
        Assert.Equal(50, query.LastFilter.PageSize);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task HandleGetAll_WhenPagingIsInvalid_ThrowsValidationException(int page, int pageSize)
    {
        var handler = new GetCategoriesHandler(new FakeCategoryQuery());

        await Assert.ThrowsAsync<CategoryValidationException>(() =>
            handler.Handle(new GetCategoriesQuery(page, pageSize), CancellationToken.None));
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
        public CategoryListFilter? LastFilter { get; private set; }

        public Task<PagedResult<Category>> GetPageAsync(
            CategoryListFilter filter,
            CancellationToken cancellationToken)
        {
            LastFilter = filter;
            return Task.FromResult(new PagedResult<Category>(
                categories ?? [], filter.Page, filter.PageSize, categories?.Count ?? 0));
        }

        public Task<Category?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(category?.Id == id && category.DeletedAt is null
                && category.Status == CatalogStatus.ACTIVE
                    ? category
                    : null);
    }
}
