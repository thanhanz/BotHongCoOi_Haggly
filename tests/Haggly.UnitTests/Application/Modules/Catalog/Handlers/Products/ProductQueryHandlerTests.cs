using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Handlers.Products;
using Haggly.Application.Modules.Catalog.Queries.Products;
using Haggly.Domain.Modules.Catalog;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Handlers.Products;

public sealed class ProductQueryHandlerTests
{
    [Fact]
    public async Task HandleGetAll_WhenProductsExist_ReturnsProductDtosForCategoryFilter()
    {
        var category = new Category { Name = "Fruit", Slug = "fruit" };
        var product = new Product { CategoryId = category.Id, Name = "Apple", DefaultUnit = ProductUnit.KG };
        var handler = new GetProductsHandler(new FakeProductQuery([product]));

        var result = await handler.Handle(new GetProductsQuery(category.Id), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(product.Id, item.Id);
        Assert.Equal(category.Id, item.CategoryId);
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
        public Task<IReadOnlyCollection<Product>> GetAllActiveAsync(
            Guid? categoryId,
            CancellationToken cancellationToken)
            => Task.FromResult(products ?? []);

        public Task<Product?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(product?.Id == id && product.DeletedAt is null
                && product.Status == CatalogStatus.ACTIVE
                    ? product
                    : null);
    }
}
