using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Queries.Products;
using Haggly.Domain.Modules.Catalog;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Handlers.Products;

public sealed class ProductQueryHandlerTests
{
    [Fact]
    public async Task HandleGetAll_WhenProductsExist_ReturnsPagedProductDtosForCategoryFilter()
    {
        var category = new Category { Name = "Fruit", Slug = "fruit" };
        var product = new Product { CategoryId = category.Id, Name = "Apple", DefaultUnit = ProductUnit.KG };
        var handler = new GetProductsHandler(new FakeProductQuery([product]));

        var result = await handler.Handle(new GetProductsQuery(category.Id), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(product.Id, item.Id);
        Assert.Equal(category.Id, item.CategoryId);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task HandleGetAll_WhenPagingIsProvided_PassesItToTheQuery()
    {
        var query = new FakeProductQuery();
        var handler = new GetProductsHandler(query);
        var categoryId = Guid.NewGuid();

        await handler.Handle(new GetProductsQuery(categoryId, 2, 50), CancellationToken.None);

        Assert.Equal(categoryId, query.LastFilter!.CategoryId);
        Assert.Equal(2, query.LastFilter.Page);
        Assert.Equal(50, query.LastFilter.PageSize);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task HandleGetAll_WhenPagingIsInvalid_ThrowsValidationException(int page, int pageSize)
    {
        var handler = new GetProductsHandler(new FakeProductQuery());

        await Assert.ThrowsAsync<ProductValidationException>(() =>
            handler.Handle(new GetProductsQuery(null, page, pageSize), CancellationToken.None));
    }

    [Fact]
    public async Task HandleGetById_WhenProductExists_ReturnsProductDto()
    {
        var product = new Product { CategoryId = Guid.NewGuid(), Name = "Apple", DefaultUnit = ProductUnit.KG };
        var handler = new GetProductByIdHandler(new FakeProductQuery([], product));

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Apple", result.Name);
    }

    [Fact]
    public async Task HandleGetById_WhenProductDoesNotExist_ThrowsNotFoundException()
    {
        var handler = new GetProductByIdHandler(new FakeProductQuery());

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeProductQuery(
        IReadOnlyCollection<Product>? products = null,
        Product? product = null) : IProductQuery
    {
        public ProductListFilter? LastFilter { get; private set; }

        public Task<PagedResult<Product>> GetPageAsync(
            ProductListFilter filter,
            CancellationToken cancellationToken)
        {
            LastFilter = filter;
            return Task.FromResult(new PagedResult<Product>(
                products ?? [], filter.Page, filter.PageSize, products?.Count ?? 0));
        }

        public Task<Product?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(product?.Id == id && product.DeletedAt is null
                && product.Status == CatalogStatus.ACTIVE
                    ? product
                    : null);
    }
}
