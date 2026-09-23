using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using Haggly.Domain.Modules.Catalog;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Queries;

public sealed class GetCategoryByIdHandlerTests
{
    private readonly ICategoryQuery _query = Substitute.For<ICategoryQuery>();

    [Fact]
    public async Task Handle_ActiveCategory_ReturnsMappedCategory()
    {
        // Arrange
        var category = new Category { Name = "Fruit", Slug = "fruit" };
        _query.GetActiveByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        // Act
        var result = await new GetCategoryByIdHandler(_query).Handle(
            new GetCategoryByIdQuery(category.Id), CancellationToken.None);

        // Assert
        Assert.Equal(category.Id, result.Id);
        Assert.Equal("fruit", result.Slug);
    }

    [Fact]
    public async Task Handle_MissingCategory_ThrowsNotFound()
    {
        // Arrange
        _query.GetActiveByIdAsync(CategoryId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        // Act
        var action = () => new GetCategoryByIdHandler(_query).Handle(
            new GetCategoryByIdQuery(CategoryId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CategoryNotFoundException>(action);
    }

    [Fact]
    public async Task Handle_EmptyId_ThrowsValidationWithoutQuerying()
    {
        // Arrange

        // Act
        var action = () => new GetCategoryByIdHandler(_query).Handle(
            new GetCategoryByIdQuery(Guid.Empty), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CategoryValidationException>(action);
        await _query.DidNotReceive().GetActiveByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static readonly Guid CategoryId = Guid.Parse("93000000-0000-0000-0000-000000000001");
}
