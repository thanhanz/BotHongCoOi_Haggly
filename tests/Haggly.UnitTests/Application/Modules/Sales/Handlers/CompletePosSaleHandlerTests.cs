using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.Handlers;

public sealed class CompletePosSaleHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 15, 3, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WhenInventoryIsAvailable_CreatesCompletedSaleAndDelegatesInventoryRecording()
    {
        var stallId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var inventory = new FakeInventorySaleRecorder
        {
            Items =
            [new InventorySaleItemSnapshot(
                listingId,
                "Tomato",
                ProductUnit.KG,
                45_000m,
                2.5m,
                0L,
                0L)]
        };
        var repository = new FakePosSaleCommandRepository();
        var unitOfWork = new FakePosSaleUnitOfWork();
        var handler = new CompletePosSaleHandler(
            repository,
            inventory,
            unitOfWork,
            new FixedBusinessClock(Now));

        var result = await handler.Handle(
            new CompletePosSaleCommand(
                stallId,
                actorId,
                "client-001",
                [new PosSaleLineInput(listingId, 2.5m, 0L, 0L)]),
            CancellationToken.None);

        Assert.Equal(PosSaleStatus.COMPLETED, result.Status);
        Assert.Equal(112_500m, result.TotalAmount);
        Assert.Single(repository.Sales);
        Assert.Equal(1, inventory.RecordCalls);
        Assert.Equal(1, unitOfWork.TransactionCount);
        Assert.Equal(repository.Sales[0].Id, inventory.SaleId);
    }

    [Fact]
    public async Task Handle_WhenRevenueRecorderIsAvailable_RecordsSaleRevenueBeforeCommit()
    {
        var revenue = new FakeRevenueSaleRecorder();
        var repository = new FakePosSaleCommandRepository();
        var inventory = new FakeInventorySaleRecorder
        {
            Items =
            [new InventorySaleItemSnapshot(
                Guid.NewGuid(), "Tomato", ProductUnit.KG, 45_000m, 1m, 0L, 0L)]
        };
        var handler = new CompletePosSaleHandler(
            repository,
            inventory,
            new FakePosSaleUnitOfWork(),
            new FixedBusinessClock(Now),
            revenue);

        var result = await handler.Handle(
            new CompletePosSaleCommand(
                Guid.NewGuid(), Guid.NewGuid(), "client-revenue",
                [new PosSaleLineInput(inventory.Items[0].InventoryItemId, 1m, 0L, 0L)]),
            CancellationToken.None);

        var entry = Assert.Single(revenue.Entries);
        Assert.Equal(result.Id, entry.SaleId);
        Assert.Equal(result.TotalAmount, entry.GrossAmount);
    }

    [Fact]
    public async Task Handle_WhenSameClientRequestWasAlreadyCompleted_ReturnsExistingSaleWithoutRecordingInventory()
    {
        var existing = PosSale.Complete(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "client-001",
            [new PosSaleItemInput(
                Guid.NewGuid(),
                "Tomato",
                ProductUnit.KG,
                45_000m,
                1m)],
            Now);
        var repository = new FakePosSaleCommandRepository { Existing = existing };
        var inventory = new FakeInventorySaleRecorder();
        var unitOfWork = new FakePosSaleUnitOfWork();
        var handler = new CompletePosSaleHandler(
            repository,
            inventory,
            unitOfWork,
            new FixedBusinessClock(Now));

        var result = await handler.Handle(
            new CompletePosSaleCommand(
                existing.StallId,
                existing.CompletedBy,
                "client-001",
                [new PosSaleLineInput(Guid.NewGuid(), 5m, 0L, 0L)]),
            CancellationToken.None);

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(0, inventory.RecordCalls);
        Assert.Equal(0, unitOfWork.TransactionCount);
    }

    [Fact]
    public async Task Handle_WhenListingVersionIsStale_ThrowsConflictException()
    {
        var inventory = new FakeInventorySaleRecorder
        {
            Failure = new PosSaleConflictException("The listing was changed by another request.")
        };
        var handler = new CompletePosSaleHandler(
            new FakePosSaleCommandRepository(),
            inventory,
            new FakePosSaleUnitOfWork(),
            new FixedBusinessClock(Now));

        await Assert.ThrowsAsync<PosSaleConflictException>(() => handler.Handle(
            new CompletePosSaleCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "client-001",
                [new PosSaleLineInput(Guid.NewGuid(), 1m, 0L, 0L)]),
            CancellationToken.None));
    }

    private sealed class FixedBusinessClock(DateTimeOffset now) : IBusinessClock
    {
        public DateTimeOffset GetNow() => now;
        public DateOnly GetBusinessDate() => DateOnly.FromDateTime(now.DateTime);
    }

    private sealed class FakePosSaleCommandRepository : IPosSaleCommandRepository
    {
        public PosSale? Existing { get; init; }
        public List<PosSale> Sales { get; } = [];

        public Task<PosSale?> FindByClientRequestIdAsync(
            Guid stallId,
            string clientRequestId,
            CancellationToken cancellationToken)
            => Task.FromResult(Existing?.StallId == stallId
                && Existing.ClientRequestId == clientRequestId
                ? Existing
                : null);

        public Task AddAsync(PosSale sale, CancellationToken cancellationToken)
        {
            Sales.Add(sale);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeInventorySaleRecorder : IInventorySaleRecorder
    {
        public IReadOnlyList<InventorySaleItemSnapshot> Items { get; init; } = [];
        public Exception? Failure { get; init; }
        public int RecordCalls { get; private set; }
        public Guid SaleId { get; private set; }

        public Task<IReadOnlyList<InventorySaleItemSnapshot>> RecordPosSaleAsync(
            Guid stallId,
            Guid saleId,
            Guid actorId,
            IReadOnlyCollection<InventorySaleLine> lines,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken)
        {
            RecordCalls++;
            SaleId = saleId;
            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.FromResult(Items);
        }
    }

    private sealed class FakeRevenueSaleRecorder : IRevenueSaleRecorder
    {
        public List<CompletedPosSaleRevenue> Entries { get; } = [];

        public Task RecordCompletedPosSaleAsync(
            CompletedPosSaleRevenue revenue,
            CancellationToken cancellationToken)
        {
            Entries.Add(revenue);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePosSaleUnitOfWork : IPosSaleUnitOfWork
    {
        public int TransactionCount { get; private set; }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            TransactionCount++;
            return await operation(cancellationToken);
        }
    }
}
