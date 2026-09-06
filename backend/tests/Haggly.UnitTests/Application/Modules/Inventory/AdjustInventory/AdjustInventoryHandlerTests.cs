using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Application.Modules.Inventory.AdjustInventory;

public sealed class AdjustInventoryHandlerTests
{
    private readonly IInventoryCommandRepository _repository = Substitute.For<IInventoryCommandRepository>();

    private readonly IInventoryReferenceQuery _references = Substitute.For<IInventoryReferenceQuery>();

    private readonly IInventoryUnitOfWork _unitOfWork = Substitute.For<IInventoryUnitOfWork>();

    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_ValidAdjustment_AdjustsInventory()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new AdjustInventoryCommand(
            fixture.Stall.Id,
            fixture.Item.Id,
            fixture.OwnerId,
            2m,
            "Delivery",
            fixture.Item.Version);
        ConfigureFixture(fixture);

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(7m, result.CurrentQuantity);
        Assert.Equal(7m, fixture.Item.CurrentQuantity);
        Assert.Equal(1, fixture.Item.Version);
        await _repository.Received(1).FindItemAsync(
            fixture.Stall.Id,
            fixture.Item.Id,
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<InventoryItemDto>>>(),
            Arg.Any<CancellationToken>());
        _clock.Received(1).GetNow();
    }

    [Fact]
    public async Task Handle_InventoryDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureStall(fixture.Stall);
        _repository.FindItemAsync(
                fixture.Stall.Id,
                fixture.Item.Id,
                Arg.Any<CancellationToken>())
            .Returns((InventoryItem?)null);
        ConfigureTransaction();
        var command = CreateCommand(fixture, 1m, fixture.Item.Version);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    public async Task Handle_InvalidQuantity_ThrowsValidationException(decimal quantityDelta)
    {
        // Arrange
        var fixture = CreateFixture();
        var command = CreateCommand(fixture, quantityDelta, fixture.Item.Version);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryValidationException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _references.DidNotReceive().FindActiveStallAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StallDoesNotBelongToVendor_ThrowsAuthorizationException()
    {
        // Arrange
        var fixture = CreateFixture();
        var otherOwnerId = Guid.Parse("70000000-0000-0000-0000-000000000003");
        fixture.Stall.VendorId = otherOwnerId;
        ConfigureStall(fixture.Stall);
        ConfigureTransaction();
        var command = CreateCommand(fixture, 1m, fixture.Item.Version);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryForbiddenException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().FindItemAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VersionDoesNotMatch_ThrowsConflictException()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);
        var command = CreateCommand(fixture, 1m, fixture.Item.Version + 1);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryConflictException>(action);
        Assert.Equal(5m, fixture.Item.CurrentQuantity);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var fixture = CreateFixture();
        var cancellationToken = new CancellationToken(canceled: true);
        ConfigureStall(fixture.Stall);
        _references.FindActiveStallAsync(fixture.Stall.Id, cancellationToken)
            .Returns(Task.FromCanceled<Stall?>(cancellationToken));
        var command = CreateCommand(fixture, 1m, fixture.Item.Version);

        // Act
        var action = () => CreateSubject().Handle(command, cancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _references.Received(1).FindActiveStallAsync(
            fixture.Stall.Id, cancellationToken);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdjustmentWouldViolateReservedQuantity_ThrowsConflictException()
    {
        // Arrange
        var fixture = CreateFixture(currentQuantity: 3m);
        fixture.Item.Reserve(3m, fixture.Now);
        ConfigureFixture(fixture);
        var command = CreateCommand(fixture, -1m, fixture.Item.Version);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryConflictException>(action);
        Assert.Equal(3m, fixture.Item.CurrentQuantity);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private AdjustInventoryHandler CreateSubject()
        => new(_repository, _references, _unitOfWork, _clock);

    private void ConfigureFixture(InventoryFixture fixture)
    {
        ConfigureStall(fixture.Stall);
        _repository.FindItemAsync(
                fixture.Stall.Id,
                fixture.Item.Id,
                Arg.Any<CancellationToken>())
            .Returns(fixture.Item);
        _clock.GetNow().Returns(fixture.Now);
        ConfigureTransaction();
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

    private static AdjustInventoryCommand CreateCommand(
        InventoryFixture fixture,
        decimal quantityDelta,
        long expectedVersion)
        => new(
            fixture.Stall.Id,
            fixture.Item.Id,
            fixture.OwnerId,
            quantityDelta,
            "Delivery",
            expectedVersion);

    private static InventoryFixture CreateFixture(decimal currentQuantity = 5m)
    {
        var ownerId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var stall = new Stall { VendorId = ownerId, Status = StallStatus.ACTIVE };
        var now = new DateTimeOffset(2026, 8, 30, 4, 0, 0, TimeSpan.Zero);
        var inventory = DomainInventory.Create(stall.Id, ownerId, now);
        var item = inventory.AddItem(
            Guid.Parse("70000000-0000-0000-0000-000000000002"),
            currentQuantity,
            ownerId,
            now);

        return new InventoryFixture(ownerId, stall, item, now);
    }

    private sealed record InventoryFixture(
        Guid OwnerId,
        Stall Stall,
        InventoryItem Item,
        DateTimeOffset Now);
}
