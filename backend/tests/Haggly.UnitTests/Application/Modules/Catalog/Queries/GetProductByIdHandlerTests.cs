using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Queries.Products;
using Haggly.Domain.Modules.Catalog;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Queries;

public sealed class GetProductByIdHandlerTests
{
    private readonly IProductQuery _query = Substitute.For<IProductQuery>();

    [Fact]
    public async Task Handle_ActiveProduct_ReturnsMappedProduct()
    {
        // Arrange
        var product = new Product { CategoryId = CategoryId, Name = "Apple", DefaultUnit = ProductUnit.KG };
        _query.GetActiveByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        // Act
        var result = await new GetProductByIdHandler(_query).Handle(
            new GetProductByIdQuery(product.Id), CancellationToken.None);

        // Assert
        Assert.Equal(product.Id, result.Id);
        Assert.Equal(CategoryId, result.CategoryId);
        Assert.Equal("Apple", result.Name);
    }

    [Fact]
    public async Task Handle_MissingProduct_ThrowsNotFound()
    {
        // Arrange
        _query.GetActiveByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        // Act
        var action = () => new GetProductByIdHandler(_query).Handle(
            new GetProductByIdQuery(ProductId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(action);
    }

    [Fact]
    public async Task Handle_EmptyId_ThrowsValidationWithoutQuerying()
    {
        // Arrange

        // Act
        var action = () => new GetProductByIdHandler(_query).Handle(
            new GetProductByIdQuery(Guid.Empty), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductValidationException>(action);
        await _query.DidNotReceive().GetActiveByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static readonly Guid CategoryId = Guid.Parse("94000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductId = Guid.Parse("94000000-0000-0000-0000-000000000002");
}
