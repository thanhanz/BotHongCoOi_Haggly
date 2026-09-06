using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using Haggly.Domain.Modules.Catalog;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Queries;

public sealed class GetCategoriesHandlerTests
{
    private readonly ICategoryQuery _query = Substitute.For<ICategoryQuery>();

    [Fact]
    public async Task Handle_ValidPage_ReturnsMappedPageAndForwardsPaging()
    {
        // Arrange
        var category = new Category { Name = "Fruit", Slug = "fruit" };
        _query.GetPageAsync(Arg.Any<CategoryListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Category>([category], 2, 10, 21));

        // Act
        var result = await new GetCategoriesHandler(_query)
            .Handle(new GetCategoriesQuery(2, 10), CancellationToken.None);

        // Assert
        var mapped = Assert.Single(result.Items);
        Assert.Equal(category.Id, mapped.Id);
        Assert.Equal("Fruit", mapped.Name);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(21, result.TotalCount);
        await _query.Received(1).GetPageAsync(
            Arg.Is<CategoryListFilter>(filter => filter.Page == 2 && filter.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Handle_InvalidPaging_ThrowsValidationException(int page, int pageSize)
    {
        // Arrange
        var handler = new GetCategoriesHandler(_query);

        // Act
        var action = () => handler.Handle(new GetCategoriesQuery(page, pageSize), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CategoryValidationException>(action);
        await _query.DidNotReceive().GetPageAsync(
            Arg.Any<CategoryListFilter>(), Arg.Any<CancellationToken>());
    }
}
