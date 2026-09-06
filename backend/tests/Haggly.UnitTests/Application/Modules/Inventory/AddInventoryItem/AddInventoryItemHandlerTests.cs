using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Application.Modules.Inventory.AddInventoryItem;

public sealed class AddInventoryItemHandlerTests
{
    private readonly IInventoryCommandRepository _repository = Substitute.For<IInventoryCommandRepository>();

    private readonly IInventoryReferenceQuery _references = Substitute.For<IInventoryReferenceQuery>();

    private readonly IInventoryUnitOfWork _unitOfWork = Substitute.For<IInventoryUnitOfWork>();

    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_ValidProductListing_AddsInventoryItem()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture, itemExists: false);
        var command = CreateCommand(fixture, 12m);

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(12m, result.CurrentQuantity);
        Assert.Equal(12m, Assert.Single(fixture.Inventory.Items).CurrentQuantity);
        await _repository.Received(1).AddItemAsync(
            Arg.Is<InventoryItem>(item =>
                item.ProductStallId == fixture.ProductStall.Id
                && item.CurrentQuantity == 12m),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InventoryDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureStallAndProduct(fixture);
        _repository.FindInventoryAsync(fixture.Stall.Id, Arg.Any<CancellationToken>())
            .Returns((DomainInventory?)null);
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().Handle(CreateCommand(fixture, 12m), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryNotFoundException>(action);
        await _repository.DidNotReceive().AddItemAsync(
            Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductListingDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureStall(fixture.Stall);
        _repository.FindInventoryAsync(fixture.Stall.Id, Arg.Any<CancellationToken>())
            .Returns(fixture.Inventory);
        _references.FindActiveProductStallAsync(
                fixture.Stall.Id,
                fixture.ProductStall.Id,
                Arg.Any<CancellationToken>())
            .Returns((ProductStall?)null);
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().Handle(CreateCommand(fixture, 12m), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryNotFoundException>(action);
        await _repository.DidNotReceive().AddItemAsync(
            Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StallDoesNotBelongToVendor_ThrowsAuthorizationException()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Stall.VendorId = Guid.Parse("71000000-0000-0000-0000-000000000003");
        ConfigureStall(fixture.Stall);
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().Handle(CreateCommand(fixture, 12m), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryForbiddenException>(action);
        await _repository.DidNotReceive().FindInventoryAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-1)]
    public async Task Handle_InvalidQuantity_ThrowsValidationException(decimal quantity)
    {
        // Arrange
        var fixture = CreateFixture();

        // Act
        var action = () => CreateSubject().Handle(CreateCommand(fixture, quantity), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryValidationException>(action);
        await _references.DidNotReceive().FindActiveStallAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ListingAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture, itemExists: true);

        // Act
        var action = () => CreateSubject().Handle(CreateCommand(fixture, 12m), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryConflictException>(action);
        await _repository.DidNotReceive().AddItemAsync(
            Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DomainRejectsDuplicateItem_PropagatesDomainFailure()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Inventory.AddItem(
            fixture.ProductStall.Id,
            2m,
            fixture.OwnerId,
            fixture.Now);
        ConfigureFixture(fixture, itemExists: false);

        // Act
        var action = () => CreateSubject().Handle(CreateCommand(fixture, 12m), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
        await _repository.DidNotReceive().AddItemAsync(
            Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var fixture = CreateFixture();
        var cancellationToken = new CancellationToken(canceled: true);
        _references.FindActiveStallAsync(fixture.Stall.Id, cancellationToken)
            .Returns(Task.FromCanceled<Stall?>(cancellationToken));

        // Act
        var action = () => CreateSubject().Handle(CreateCommand(fixture, 12m), cancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _references.Received(1).FindActiveStallAsync(
            fixture.Stall.Id, cancellationToken);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private AddInventoryItemHandler CreateSubject()
        => new(_repository, _references, _unitOfWork, _clock);

    private void ConfigureFixture(InventoryFixture fixture, bool itemExists)
    {
        ConfigureStallAndProduct(fixture);
        _repository.FindInventoryAsync(fixture.Stall.Id, Arg.Any<CancellationToken>())
            .Returns(fixture.Inventory);
        _repository.ItemExistsAsync(
                fixture.Inventory.Id,
                fixture.ProductStall.Id,
                Arg.Any<CancellationToken>())
            .Returns(itemExists);
        _clock.GetNow().Returns(fixture.Now);
        ConfigureTransaction();
    }

    private void ConfigureStallAndProduct(InventoryFixture fixture)
    {
        ConfigureStall(fixture.Stall);
        _references.FindActiveProductStallAsync(
                fixture.Stall.Id,
                fixture.ProductStall.Id,
                Arg.Any<CancellationToken>())
            .Returns(fixture.ProductStall);
    }

    private void ConfigureStall(Stall stall)
        => _references.FindActiveStallAsync(
                stall.Id,
                Arg.Any<CancellationToken>())
            .Returns(stall);

    private void ConfigureTransaction()
        => _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<InventoryItemDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<InventoryItemDto>>>();
                var cancellationToken = callInfo.ArgAt<CancellationToken>(1);
                return operation(cancellationToken);
            });

    private static AddInventoryItemCommand CreateCommand(
        InventoryFixture fixture,
        decimal quantity)
        => new(fixture.Stall.Id, fixture.OwnerId, fixture.ProductStall.Id, quantity);

    private static InventoryFixture CreateFixture()
    {
        var ownerId = Guid.Parse("71000000-0000-0000-0000-000000000001");
        var stall = new Stall { VendorId = ownerId, Status = StallStatus.ACTIVE };
        var product = new Product
        {
            Name = "Tomato",
            Status = CatalogStatus.ACTIVE
        };
        var productStall = ProductStall.Create(
            stall.Id,
            product.Id,
            "Tomato",
            ProductUnit.KG,
            1m,
            40_000m,
            false);
        productStall.Product = product;
        var now = new DateTimeOffset(2026, 8, 30, 4, 0, 0, TimeSpan.Zero);
        var inventory = DomainInventory.Create(stall.Id, ownerId, now);

        return new InventoryFixture(ownerId, stall, productStall, inventory, now);
    }

    private sealed record InventoryFixture(
        Guid OwnerId,
        Stall Stall,
        ProductStall ProductStall,
        DomainInventory Inventory,
        DateTimeOffset Now);
}
