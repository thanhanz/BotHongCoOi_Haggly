using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.ProductStalls;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.UpdateProductStall;

public sealed class UpdateProductStallHandlerTests
{
    private readonly IProductStallCommandRepository _repository = Substitute.For<IProductStallCommandRepository>();

    [Fact]
    public async Task Handle_OwnedListing_UpdatesConfigurationAndSaves()
    {
        // Arrange
        var listing = ProductStall.Create(StallId, ProductId, "Apple", ProductUnit.KG, 1m, 10m, false);
        _repository.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(new Stall { VendorId = VendorId });
        _repository.FindActiveAsync(listing.Id, Arg.Any<CancellationToken>()).Returns(listing);

        // Act
        var result = await CreateSubject().Handle(
            new UpdateProductStallCommand(StallId, listing.Id, VendorId, " Red Apple ", ProductUnit.PIECE, 2m, 12.5m, true, true, 0), CancellationToken.None);

        // Assert
        Assert.Equal("Red Apple", result.DisplayName);
        Assert.Equal(ProductUnit.PIECE, listing.SellingUnit);
        Assert.Equal(2m, listing.MinimumOrderQuantity);
        Assert.Equal(12.5m, listing.CurrentUnitPrice);
        Assert.Equal(1, listing.Version);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OtherVendor_ThrowsForbiddenWithoutSaving()
    {
        // Arrange
        _repository.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(new Stall { VendorId = OtherVendorId });

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateProductStallCommand(StallId, ListingId, VendorId, null, null, null, null, null, null, 0), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductStallForbiddenException>(action);
        await _repository.DidNotReceive().FindActiveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ListingDoesNotExist_ThrowsNotFoundWithoutSaving()
    {
        // Arrange
        _repository.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(new Stall { VendorId = VendorId });
        _repository.FindActiveAsync(ListingId, Arg.Any<CancellationToken>()).Returns((ProductStall?)null);

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateProductStallCommand(StallId, ListingId, VendorId, null, null, null, null, null, null, 0), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductStallNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VersionDoesNotMatch_ThrowsConflictWithoutSaving()
    {
        // Arrange
        var listing = ProductStall.Create(StallId, ProductId, "Apple", ProductUnit.KG, 1m, 10m, false);
        _repository.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(new Stall { VendorId = VendorId });
        _repository.FindActiveAsync(listing.Id, Arg.Any<CancellationToken>()).Returns(listing);

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateProductStallCommand(StallId, listing.Id, VendorId, null, null, 2m, 12m, null, null, 1), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductStallConflictException>(action);
        Assert.Equal(10m, listing.CurrentUnitPrice);
        Assert.Equal(0, listing.Version);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, -1)]
    public async Task Handle_InvalidQuantityOrPrice_ThrowsValidationWithoutSaving(decimal quantity, decimal price)
    {
        // Arrange
        _repository.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(new Stall { VendorId = VendorId });
        var listing = ProductStall.Create(StallId, ProductId, "Apple", ProductUnit.KG, 1m, 10m, false);
        _repository.FindActiveAsync(listing.Id, Arg.Any<CancellationToken>()).Returns(listing);

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateProductStallCommand(StallId, listing.Id, VendorId, null, null, quantity, price, null, null, 0), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductStallValidationException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private UpdateProductStallHandler CreateSubject() => new(_repository);
    private static readonly Guid StallId = Guid.Parse("84000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductId = Guid.Parse("84000000-0000-0000-0000-000000000002");
    private static readonly Guid ListingId = Guid.Parse("84000000-0000-0000-0000-000000000003");
    private static readonly Guid VendorId = Guid.Parse("84000000-0000-0000-0000-000000000004");
    private static readonly Guid OtherVendorId = Guid.Parse("84000000-0000-0000-0000-000000000005");
}
