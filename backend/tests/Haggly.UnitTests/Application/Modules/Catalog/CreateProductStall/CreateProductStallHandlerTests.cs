using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Commands.ProductStalls;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Catalog.CreateProductStall;

public sealed class CreateProductStallHandlerTests
{
    private readonly IProductStallCommandRepository _repository = Substitute.For<IProductStallCommandRepository>();

    [Fact]
    public async Task Handle_OwnedActiveStallAndProduct_CreatesListing()
    {
        // Arrange
        var stallId = Guid.Parse("A3000000-0000-0000-0000-000000000001");
        var productId = Guid.Parse("A3000000-0000-0000-0000-000000000002");
        var actorId = Guid.Parse("A3000000-0000-0000-0000-000000000003");
        _repository.FindActiveStallAsync(stallId, Arg.Any<CancellationToken>()).Returns(new Stall { VendorId = actorId });
        _repository.FindActiveProductAsync(productId, Arg.Any<CancellationToken>()).Returns(new Product { Status = CatalogStatus.ACTIVE });
        _repository.ExistsAsync(stallId, productId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await new CreateProductStallHandler(_repository).Handle(
            new CreateProductStallCommand(stallId, productId, actorId, "Tomato", ProductUnit.KG, 1m, 45_000m, true), CancellationToken.None);

        // Assert
        Assert.Equal(stallId, result.StallId);
        Assert.Equal(45_000m, result.CurrentUnitPrice);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StallBelongsToAnotherVendor_ThrowsForbiddenWithoutSaving()
    {
        // Arrange
        var stallId = Guid.Parse("A3000000-0000-0000-0000-000000000001");
        var actorId = Guid.Parse("A3000000-0000-0000-0000-000000000003");
        _repository.FindActiveStallAsync(stallId, Arg.Any<CancellationToken>()).Returns(new Stall { VendorId = Guid.Parse("A3000000-0000-0000-0000-000000000004") });

        // Act
        var action = () => new CreateProductStallHandler(_repository).Handle(
            new CreateProductStallCommand(stallId, Guid.Parse("A3000000-0000-0000-0000-000000000002"), actorId, "Tomato", ProductUnit.KG, 1m, 45_000m, true), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProductStallForbiddenException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
