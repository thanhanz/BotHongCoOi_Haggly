using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using Haggly.Application.Modules.Catalog.Queries.ProductStalls;
using Haggly.Domain.Modules.Catalog;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.Queries;

public sealed class GetProductStallByIdHandlerTests
{
    private readonly IProductStallQuery _query = Substitute.For<IProductStallQuery>();

    [Fact]
    public async Task Handle_ActiveListing_ReturnsMappedListing()
    {
        // Arrange
        var listing = ProductStall.Create(StallId, ProductId, "Apple", ProductUnit.KG, 1m, 20m, true);
        _query.GetActiveByIdAsync(StallId, listing.Id, Arg.Any<CancellationToken>()).Returns(listing);

        // Act
        var result = await new GetProductStallByIdHandler(_query).Handle(
            new GetProductStallByIdQuery(StallId, listing.Id), CancellationToken.None);

        // Assert
        Assert.Equal(listing.Id, result.Id);
        Assert.Equal(StallId, result.StallId);
        Assert.Equal(ProductId, result.ProductId);
    }

    [Fact]
    public async Task Handle_MissingListing_ThrowsNotFound()
    {
        // Arrange
        _query.GetActiveByIdAsync(StallId, ListingId, Arg.Any<CancellationToken>()).Returns((ProductStall?)null);

        // Act
        var action = () => new GetProductStallByIdHandler(_query).Handle(
            new GetProductStallByIdQuery(StallId, ListingId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductStallNotFoundException>(action);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Handle_InvalidIdentifier_ThrowsValidationWithoutQuerying(bool emptyStall, bool emptyListing)
    {
        // Arrange
        var stallId = emptyStall ? Guid.Empty : StallId;
        var listingId = emptyListing ? Guid.Empty : ListingId;

        // Act
        var action = () => new GetProductStallByIdHandler(_query).Handle(
            new GetProductStallByIdQuery(stallId, listingId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductStallValidationException>(action);
        await _query.DidNotReceive().GetActiveByIdAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static readonly Guid StallId = Guid.Parse("96000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductId = Guid.Parse("96000000-0000-0000-0000-000000000002");
    private static readonly Guid ListingId = Guid.Parse("96000000-0000-0000-0000-000000000003");
}
