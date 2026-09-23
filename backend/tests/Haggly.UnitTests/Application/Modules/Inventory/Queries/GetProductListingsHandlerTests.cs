using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Queries;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Inventory.Queries;

public sealed class GetProductListingsHandlerTests
{
    private readonly IProductListingReader reader = Substitute.For<IProductListingReader>();

    [Fact]
    public async Task Handle_ValidRequest_ReturnsRepositoryPage()
    {
        // Arrange
        var categoryId = Guid.Parse("B1000000-0000-0000-0000-000000000001");
        var stallId = Guid.Parse("B1000000-0000-0000-0000-000000000002");
        var expected = new PagedResult<ProductListingDto>([], 2, 10, 12);
        reader.GetPageAsync(Arg.Any<ProductListingListFilter>(), Arg.Any<CancellationToken>())
            .Returns(expected);
        var handler = new GetProductListingsHandler(reader);

        // Act
        var result = await handler.Handle(
            new GetProductListingsQuery(categoryId, stallId, null, 2, 10),
            CancellationToken.None);

        // Assert
        Assert.Same(expected, result);
        await reader.Received(1).GetPageAsync(
            Arg.Is<ProductListingListFilter>(filter =>
                filter.CategoryId == categoryId
                && filter.StallId == stallId
                && filter.Page == 2
                && filter.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Handle_InvalidPagination_ThrowsInventoryValidationException(int page, int pageSize)
    {
        // Arrange
        var handler = new GetProductListingsHandler(reader);

        // Act
        var action = () => handler.Handle(
            new GetProductListingsQuery(null, null, null, page, pageSize),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryValidationException>(action);
        await reader.DidNotReceive().GetPageAsync(
            Arg.Any<ProductListingListFilter>(), Arg.Any<CancellationToken>());
    }
}
