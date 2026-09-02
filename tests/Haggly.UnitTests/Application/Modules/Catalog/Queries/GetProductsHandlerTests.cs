using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Queries.Products;
using Haggly.Domain.Modules.Catalog;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Queries;

public sealed class GetProductsHandlerTests
{
    private readonly IProductQuery _query = Substitute.For<IProductQuery>();

    [Fact]
    public async Task Handle_ValidCategoryAndPage_ReturnsMappedPageAndForwardsFilter()
    {
        // Arrange
        var categoryId = Guid.Parse("A3000000-0000-0000-0000-000000000001");
        var product = new Product { CategoryId = categoryId, Name = "Apple", DefaultUnit = ProductUnit.KG };
        _query.GetPageAsync(Arg.Any<ProductListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Product>([product], 2, 5, 6));

        // Act
        var result = await new GetProductsHandler(_query)
            .Handle(new GetProductsQuery(categoryId, 2, 5), CancellationToken.None);

        // Assert
        var mapped = Assert.Single(result.Items);
        Assert.Equal(product.Id, mapped.Id);
        Assert.Equal(categoryId, mapped.CategoryId);
        Assert.Equal("Apple", mapped.Name);
        Assert.Equal(6, result.TotalCount);
        await _query.Received(1).GetPageAsync(
            Arg.Is<ProductListFilter>(filter =>
                filter.CategoryId == categoryId && filter.Page == 2 && filter.PageSize == 5),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1, 20)]
    [InlineData(1, 0, 20)]
    [InlineData(1, 1, 101)]
    public async Task Handle_InvalidFilter_ThrowsValidationException(int categoryValue, int page, int pageSize)
    {
        // Arrange
        var categoryId = categoryValue == 0 ? Guid.Empty : Guid.Parse("A3000000-0000-0000-0000-000000000001");
        var handler = new GetProductsHandler(_query);

        // Act
        var action = () => handler.Handle(new GetProductsQuery(categoryId, page, pageSize), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductValidationException>(action);
        await _query.DidNotReceive().GetPageAsync(
            Arg.Any<ProductListFilter>(), Arg.Any<CancellationToken>());
    }
}
