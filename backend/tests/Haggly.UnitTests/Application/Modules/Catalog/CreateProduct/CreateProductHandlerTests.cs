using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.Products;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Domain.Modules.Catalog;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.CreateProduct;

public sealed class CreateProductHandlerTests
{
    private readonly IProductCommandRepository _repository = Substitute.For<IProductCommandRepository>();

    [Fact]
    public async Task Handle_ValidProduct_CreatesAndSavesProduct()
    {
        // Arrange
        var categoryId = Guid.Parse("A2000000-0000-0000-0000-000000000001");
        _repository.FindActiveCategoryByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns(new Category { Status = CatalogStatus.ACTIVE });
        _repository.NameExistsAsync(categoryId, "Tomato", null, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await new CreateProductHandler(_repository).Handle(
            new CreateProductCommand(categoryId, " Tomato ", null, ProductUnit.KG, null), CancellationToken.None);

        // Assert
        Assert.Equal("Tomato", result.Name);
        Assert.Equal(CatalogStatus.ACTIVE, result.Status);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CategoryDoesNotExist_ThrowsNotFoundWithoutSaving()
    {
        // Arrange
        var categoryId = Guid.Parse("A2000000-0000-0000-0000-000000000001");
        _repository.FindActiveCategoryByIdAsync(categoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        // Act
        var action = () => new CreateProductHandler(_repository).Handle(
            new CreateProductCommand(categoryId, "Tomato", null, ProductUnit.KG, null), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
