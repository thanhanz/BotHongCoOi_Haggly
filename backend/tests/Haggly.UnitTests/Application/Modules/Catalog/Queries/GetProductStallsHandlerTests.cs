using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using Haggly.Application.Modules.Catalog.Queries.ProductStalls;
using Haggly.Domain.Modules.Catalog;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Queries;

public sealed class GetProductStallsHandlerTests
{
    private readonly IProductStallQuery _query = Substitute.For<IProductStallQuery>();

    [Fact]
    public async Task Handle_ValidPaging_ReturnsMappedPageAndForwardsFilter()
    {
        // Arrange
        var listing = ProductStall.Create(StallId, ProductId, "Apple", ProductUnit.KG, 1m, 10m, false);
        _query.GetProductsStallAsync(Arg.Any<ProductStallListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductStall>([listing], 2, 10, 11));

        // Act
        var result = await new GetProductStallsHandler(_query).Handle(
            new GetProductStallsQuery(StallId, 2, 10), CancellationToken.None);

        // Assert
        Assert.Equal(listing.Id, Assert.Single(result.Items).Id);
        Assert.Equal(11, result.TotalCount);
        await _query.Received(1).GetProductsStallAsync(
            Arg.Is<ProductStallListFilter>(filter =>
                filter.StallId == StallId && filter.Page == 2 && filter.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1, 20)]
    [InlineData(1, 0, 20)]
    [InlineData(1, 1, 101)]
    public async Task Handle_InvalidFilter_ThrowsValidationWithoutQuerying(int stallValue, int page, int pageSize)
    {
        // Arrange
        var stallId = stallValue == 0 ? Guid.Empty : StallId;

        // Act
        var action = () => new GetProductStallsHandler(_query).Handle(
            new GetProductStallsQuery(stallId, page, pageSize), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductStallValidationException>(action);
        await _query.DidNotReceive().GetProductsStallAsync(
            Arg.Any<ProductStallListFilter>(), Arg.Any<CancellationToken>());
    }

    private static readonly Guid StallId = Guid.Parse("95000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductId = Guid.Parse("95000000-0000-0000-0000-000000000002");
}
