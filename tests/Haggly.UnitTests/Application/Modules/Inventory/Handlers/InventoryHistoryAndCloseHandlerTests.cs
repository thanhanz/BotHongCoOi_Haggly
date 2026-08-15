using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Handlers;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Inventory.Handlers;

public sealed class InventoryHistoryAndCloseHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 15, 2, 30, 0, TimeSpan.Zero);

    private static readonly DateOnly BusinessDate = new(2026, 8, 15);

    [Fact]
    public async Task HandleLedger_WhenOwnedStallIsValid_ReturnsMappedPageAndForwardsFilters()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        var listing = DailyProductListing.Open(
            session.Id,
            Guid.NewGuid(),
            "Tomato",
            ProductUnit.KG,
            45_000m,
            10m,
            ownerId,
            Now);
        var ledger = Assert.Single(listing.InventoryLedgers);
        var query = new FakeInventoryQuery
        {
            LedgerResult = new PagedResult<InventoryLedger>([ledger], 2, 1, 3)
        };
        var handler = new GetInventoryLedgerHandler(
            query,
            new FakeReferenceQuery(stall));
        var listingId = listing.Id;

        var result = await handler.Handle(
            new GetInventoryLedgerQuery(
                stall.Id,
                ownerId,
                BusinessDate,
                listingId,
                InventoryTransactionType.OPENING,
                2,
                1),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(ledger.Id, item.Id);
        Assert.Equal(InventoryTransactionType.OPENING, item.TransactionType);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.NotNull(query.LastLedgerFilter);
        Assert.Equal(stall.Id, query.LastLedgerFilter!.StallId);
        Assert.Equal(BusinessDate, query.LastLedgerFilter.BusinessDate);
        Assert.Equal(listingId, query.LastLedgerFilter.ListingId);
        Assert.Equal(InventoryTransactionType.OPENING, query.LastLedgerFilter.TransactionType);
    }

    [Fact]
    public async Task HandleClose_WhenSessionIsOpen_ClosesAndSavesSession()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        var repository = new FakeCommandRepository { Session = session };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CloseInventorySessionHandler(
            repository,
            new FakeReferenceQuery(stall),
            unitOfWork,
            new FixedBusinessClock(Now.AddHours(8), BusinessDate));

        var result = await handler.Handle(
            new CloseInventorySessionCommand(stall.Id, ownerId),
            CancellationToken.None);

        Assert.Equal(InventorySessionStatus.CLOSED, result.Status);
        Assert.Equal(Now.AddHours(8), result.ClosedAt);
        Assert.Equal(ownerId, result.ClosedBy);
        Assert.True(repository.Saved);
        Assert.Equal(1, unitOfWork.TransactionCount);
    }

    [Fact]
    public async Task HandleClose_WhenSessionIsAlreadyClosed_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        session.Close(ownerId, Now.AddHours(8));
        var handler = new CloseInventorySessionHandler(
            new FakeCommandRepository { Session = session },
            new FakeReferenceQuery(stall),
            new FakeUnitOfWork(),
            new FixedBusinessClock(Now.AddHours(9), BusinessDate));

        await Assert.ThrowsAsync<InventoryConflictException>(() => handler.Handle(
            new CloseInventorySessionCommand(stall.Id, ownerId),
            CancellationToken.None));
    }

    private static Stall CreateActiveStall(Guid ownerId)
        => new() { VendorId = ownerId, Status = StallStatus.ACTIVE };

    private sealed class FixedBusinessClock(DateTimeOffset now, DateOnly businessDate) : IBusinessClock
    {
        public DateTimeOffset GetNow() => now;

        public DateOnly GetBusinessDate() => businessDate;
    }

    private sealed class FakeReferenceQuery(Stall? stall) : IInventoryReferenceQuery
    {
        public Task<Stall?> FindActiveStallAsync(Guid stallId, CancellationToken cancellationToken)
            => Task.FromResult(stall?.Id == stallId ? stall : null);

        public Task<ProductStall?> FindActiveProductStallAsync(
            Guid stallId,
            Guid productStallId,
            CancellationToken cancellationToken)
            => Task.FromResult<ProductStall?>(null);
    }

    private sealed class FakeInventoryQuery : IInventoryQuery
    {
        public PagedResult<InventoryLedger> LedgerResult { get; init; }
            = new([], 1, 20, 0);

        public InventoryLedgerListFilter? LastLedgerFilter { get; private set; }

        public Task<InventorySession?> GetCurrentSessionAsync(
            Guid stallId,
            DateOnly businessDate,
            CancellationToken cancellationToken)
            => Task.FromResult<InventorySession?>(null);

        public Task<InventorySession?> GetPreviousSessionAsync(
            Guid stallId,
            DateOnly businessDate,
            CancellationToken cancellationToken)
            => Task.FromResult<InventorySession?>(null);

        public Task<PagedResult<InventoryLedger>> GetLedgerAsync(
            InventoryLedgerListFilter filter,
            CancellationToken cancellationToken)
        {
            LastLedgerFilter = filter;
            return Task.FromResult(LedgerResult);
        }
    }

    private sealed class FakeCommandRepository : IInventoryCommandRepository
    {
        public InventorySession? Session { get; init; }
        public bool Saved { get; private set; }

        public Task<InventorySession?> FindSessionAsync(
            Guid stallId,
            DateOnly businessDate,
            CancellationToken cancellationToken)
            => Task.FromResult(Session?.StallId == stallId
                && Session.BusinessDate == businessDate
                ? Session
                : null);

        public Task<DailyProductListing?> FindListingAsync(
            Guid stallId,
            Guid listingId,
            CancellationToken cancellationToken)
            => Task.FromResult<DailyProductListing?>(null);

        public Task<bool> ListingExistsAsync(
            Guid inventorySessionId,
            Guid productStallId,
            CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddSessionAsync(InventorySession session, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task AddListingAsync(DailyProductListing listing, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IInventoryUnitOfWork
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
