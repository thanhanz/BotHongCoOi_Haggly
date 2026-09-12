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

    [Fact]
    public async Task Handle_StallFilter_ReturnsMappedPageAndForwardsStallAndPaging()
    {
        // Arrange
        var category = new Category { Name = "Fruit", Slug = "fruit" };
        _query.GetPageByStallIdAsync(StallId, Arg.Any<CategoryListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Category>([category], 2, 10, 11));

        // Act
        var result = await new GetCategoriesHandler(_query)
            .Handle(new GetCategoriesQuery(2, 10, StallId), CancellationToken.None);

        // Assert
        Assert.Equal(category.Id, Assert.Single(result.Items).Id);
        Assert.Equal(11, result.TotalCount);
        await _query.Received(1).GetPageByStallIdAsync(
            StallId,
            Arg.Is<CategoryListFilter>(filter => filter.Page == 2 && filter.PageSize == 10),
            Arg.Any<CancellationToken>());
        await _query.DidNotReceive().GetPageAsync(
            Arg.Any<CategoryListFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyStallFilter_ThrowsValidationWithoutQuerying()
    {
        // Arrange
        var handler = new GetCategoriesHandler(_query);

        // Act
        var action = () => handler.Handle(
            new GetCategoriesQuery(StallId: Guid.Empty),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CategoryValidationException>(action);
        await _query.DidNotReceive().GetPageAsync(
            Arg.Any<CategoryListFilter>(), Arg.Any<CancellationToken>());
        await _query.DidNotReceive().GetPageByStallIdAsync(
            Arg.Any<Guid>(), Arg.Any<CategoryListFilter>(), Arg.Any<CancellationToken>());
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

    private static readonly Guid StallId = Guid.Parse("97000000-0000-0000-0000-000000000001");
}
