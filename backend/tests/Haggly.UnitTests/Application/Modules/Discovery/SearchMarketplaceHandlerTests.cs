using Haggly.Application.Abstractions.Discovery;
using Haggly.Application.Common;
using Haggly.Application.Modules.Discovery;
using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Application.Modules.Discovery.Queries;
using Haggly.Application.Modules.Inventory.Dtos;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Discovery;

public sealed class SearchMarketplaceHandlerTests
{
    private readonly IMarketplaceSearchReader reader = Substitute.For<IMarketplaceSearchReader>();

    [Fact]
    public async Task Handle_ValidRequest_NormalizesQueryAndReturnsRepositoryResult()
    {
        // Arrange
        var expected = EmptyResult(2, 5, 3, 20);
        reader.SearchAsync(Arg.Any<MarketplaceSearchFilter>(), Arg.Any<CancellationToken>())
            .Returns(expected);
        var handler = new SearchMarketplaceHandler(reader);

        // Act
        var result = await handler.Handle(
            new SearchMarketplaceQuery("  Sạp Cô An  ", 2, 5, 3, 20),
            CancellationToken.None);

        // Assert
        Assert.Same(expected, result);
        await reader.Received(1).SearchAsync(
            Arg.Is<MarketplaceSearchFilter>(filter =>
                filter.NormalizedQuery == "sap co an"
                && filter.StallPage == 2
                && filter.StallPageSize == 5
                && filter.ProductPage == 3
                && filter.ProductPageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("---")]
    public async Task Handle_QueryHasFewerThanTwoSearchableCharacters_ThrowsDiscoveryValidationException(
        string query)
    {
        // Arrange
        var handler = new SearchMarketplaceHandler(reader);

        // Act
        var action = () => handler.Handle(
            new SearchMarketplaceQuery(query, 1, 5, 1, 20),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DiscoveryValidationException>(action);
        await reader.DidNotReceive().SearchAsync(
            Arg.Any<MarketplaceSearchFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_QueryExceedsMaximumLength_ThrowsDiscoveryValidationException()
    {
        // Arrange
        var handler = new SearchMarketplaceHandler(reader);

        // Act
        var action = () => handler.Handle(
            new SearchMarketplaceQuery(new string('a', 101), 1, 5, 1, 20),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DiscoveryValidationException>(action);
        await reader.DidNotReceive().SearchAsync(
            Arg.Any<MarketplaceSearchFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 5, 1, 20)]
    [InlineData(1, 0, 1, 20)]
    [InlineData(1, 21, 1, 20)]
    [InlineData(1, 5, 0, 20)]
    [InlineData(1, 5, 1, 0)]
    [InlineData(1, 5, 1, 101)]
    public async Task Handle_InvalidPagination_ThrowsDiscoveryValidationException(
        int stallPage,
        int stallPageSize,
        int productPage,
        int productPageSize)
    {
        // Arrange
        var handler = new SearchMarketplaceHandler(reader);

        // Act
        var action = () => handler.Handle(
            new SearchMarketplaceQuery("rau", stallPage, stallPageSize, productPage, productPageSize),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DiscoveryValidationException>(action);
        await reader.DidNotReceive().SearchAsync(
            Arg.Any<MarketplaceSearchFilter>(),
            Arg.Any<CancellationToken>());
    }

    private static MarketplaceSearchResult EmptyResult(
        int stallPage,
        int stallPageSize,
        int productPage,
        int productPageSize)
        => new(
            new PagedResult<StallSearchResult>([], stallPage, stallPageSize, 0),
            new PagedResult<ProductListingDto>([], productPage, productPageSize, 0));
}
