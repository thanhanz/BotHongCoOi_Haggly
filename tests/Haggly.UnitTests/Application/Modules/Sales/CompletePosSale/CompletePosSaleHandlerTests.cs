using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Finance;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.CompletePosSale;

public sealed class CompletePosSaleHandlerTests
{
    private readonly IPosSaleCommandRepository _repository = Substitute.For<IPosSaleCommandRepository>();
    private readonly IInventorySaleRepository _inventory = Substitute.For<IInventorySaleRepository>();
    private readonly IPosSaleUnitOfWork _unitOfWork = Substitute.For<IPosSaleUnitOfWork>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();
    private readonly IRevenueLedgerRepository _revenueLedger = Substitute.For<IRevenueLedgerRepository>();

    [Fact]
    public async Task Handle_InventoryIsAvailable_CreatesCompletedSale()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);
        var command = CreateCommand(fixture);

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(PosSaleStatus.COMPLETED, result.Status);
        Assert.Equal(112_500m, result.TotalAmount);
        await _inventory.Received(1).RecordPosSaleAsync(
            fixture.StallId,
            Arg.Any<Guid>(),
            fixture.ActorId,
            Arg.Is<IReadOnlyCollection<InventorySaleLine>>(lines =>
                lines.Count == 1 && lines.Single().InventoryItemId == fixture.InventoryItemId),
            fixture.Now,
            Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(
            Arg.Is<PosSale>(sale => sale.TotalAmount == 112_500m),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RevenueRecorderConfigured_AddsRevenueEntry()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);
        var command = CreateCommand(fixture);

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await _revenueLedger.Received(1).AddAsync(
            Arg.Is<RevenueLedger>(entry =>
                entry.PosSaleId == result.Id && entry.GrossAmount == result.TotalAmount),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameClientRequestAlreadyCompleted_ReturnsExistingSaleWithoutRecordingInventory()
    {
        // Arrange
        var fixture = CreateFixture();
        var existing = PosSale.Complete(
            Guid.Parse("D0000000-0000-0000-0000-000000000006"),
            fixture.StallId,
            fixture.ActorId,
            "client-001",
            [new PosSaleItemInput(fixture.InventoryItemId, "Tomato", ProductUnit.KG, 45_000m, 1m)],
            fixture.Now);
        _repository.FindByClientRequestIdAsync(
                fixture.StallId, "client-001", Arg.Any<CancellationToken>())
            .Returns(existing);

        // Act
        var result = await CreateSubject().Handle(CreateCommand(fixture), CancellationToken.None);

        // Assert
        Assert.Equal(existing.Id, result.Id);
        await _inventory.DidNotReceive().RecordPosSaleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<InventorySaleLine>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<PosSaleDto>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InventoryVersionIsStale_PropagatesConflictWithoutSaving()
    {
        // Arrange
        var fixture = CreateFixture();
        _repository.FindByClientRequestIdAsync(
                fixture.StallId, "client-001", Arg.Any<CancellationToken>())
            .Returns((PosSale?)null);
        _clock.GetNow().Returns(fixture.Now);
        _inventory.RecordPosSaleAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<InventorySaleLine>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<InventorySaleItemSnapshot>>(
                new PosSaleConflictException("stale listing")));
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().Handle(CreateCommand(fixture), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PosSaleConflictException>(action);
        await _repository.DidNotReceive().AddAsync(Arg.Any<PosSale>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidQuantity_ThrowsValidationWithoutRecordingInventory()
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CompletePosSaleCommand(
            fixture.StallId, fixture.ActorId, "client-001",
            [new PosSaleLineInput(fixture.InventoryItemId, 0m, 0L, 0L)]);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PosSaleValidationException>(action);
        await _inventory.DidNotReceive().RecordPosSaleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<InventorySaleLine>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_ForwardsTokenToRepository()
    {
        // Arrange
        var fixture = CreateFixture();
        var cancellationToken = new CancellationToken(canceled: true);
        _repository.FindByClientRequestIdAsync(fixture.StallId, "client-001", cancellationToken)
            .Returns(Task.FromCanceled<PosSale?>(cancellationToken));

        // Act
        var action = () => CreateSubject().Handle(CreateCommand(fixture), cancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _repository.Received(1).FindByClientRequestIdAsync(
            fixture.StallId, "client-001", cancellationToken);
        await _inventory.DidNotReceive().RecordPosSaleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<InventorySaleLine>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    private CompletePosSaleHandler CreateSubject()
        => new(_repository, _inventory, _unitOfWork, _clock, _revenueLedger);

    private void ConfigureFixture(PosFixture fixture)
    {
        _repository.FindByClientRequestIdAsync(
                fixture.StallId, "client-001", Arg.Any<CancellationToken>())
            .Returns((PosSale?)null);
        _inventory.RecordPosSaleAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<InventorySaleLine>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<InventorySaleItemSnapshot>>([new(
                fixture.InventoryItemId, "Tomato", ProductUnit.KG, 45_000m, 2.5m, 0L, 0L)]);
        _clock.GetNow().Returns(fixture.Now);
        ConfigureTransaction();
    }

    private void ConfigureTransaction()
        => _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<PosSaleDto>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<PosSaleDto>>>();
                return operation(callInfo.ArgAt<CancellationToken>(1));
            });

    private static CompletePosSaleCommand CreateCommand(PosFixture fixture)
        => new(
            fixture.StallId,
            fixture.ActorId,
            "client-001",
            [new PosSaleLineInput(fixture.InventoryItemId, 2.5m, 0L, 0L)]);

    private static PosFixture CreateFixture()
        => new(
            Guid.Parse("D0000000-0000-0000-0000-000000000001"),
            Guid.Parse("D0000000-0000-0000-0000-000000000002"),
            Guid.Parse("D0000000-0000-0000-0000-000000000003"),
            new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero));

    private sealed record PosFixture(Guid StallId, Guid ActorId, Guid InventoryItemId, DateTimeOffset Now);
}
