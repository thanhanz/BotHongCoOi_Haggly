using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Inventory.Handlers;

public sealed class InventorySessionHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 15, 2, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleOpen_WhenActorOwnsActiveStallWithoutListings_CreatesEmptyOpenSession()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var repository = new FakeCommandRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateOpenHandler(stall, repository, unitOfWork);

        var result = await handler.Handle(
            new OpenInventorySessionCommand(stall.Id, ownerId, "Morning count", []),
            CancellationToken.None);

        Assert.Equal(stall.Id, result.StallId);
        Assert.Equal(new DateOnly(2026, 8, 15), result.BusinessDate);
        Assert.Equal(InventorySessionStatus.OPEN, result.Status);
        Assert.Empty(result.Listings);
        Assert.NotNull(repository.AddedSession);
        Assert.True(repository.Saved);
        Assert.Equal(1, unitOfWork.ExecutionCount);
    }

    [Fact]
    public async Task HandleOpen_WhenPriceIsOmitted_SnapshotsProductValuesAndUsesDefaultPrice()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var productStall = CreateActiveProductStall(stall.Id, "Fresh Tomato", 45_000m);
        var repository = new FakeCommandRepository();
        var references = new FakeReferenceQuery(stall, productStall);
        var handler = new OpenInventorySessionHandler(
            repository,
            references,
            new FakeUnitOfWork(),
            new FixedBusinessClock(Now, new DateOnly(2026, 8, 15)));

        var result = await handler.Handle(
            new OpenInventorySessionCommand(
                stall.Id,
                ownerId,
                null,
                [new InventoryListingInput(productStall.Id, 25.5m, null)]),
            CancellationToken.None);

        var listing = Assert.Single(result.Listings);
        Assert.Equal(productStall.Id, listing.ProductStallId);
        Assert.Equal("Fresh Tomato", listing.ProductNameSnapshot);
        Assert.Equal(ProductUnit.KG, listing.SellingUnitSnapshot);
        Assert.Equal(45_000m, listing.PublicUnitPrice);
        Assert.Equal(25.5m, listing.OpeningQuantity);
        Assert.Single(repository.AddedListings);
    }

    [Fact]
    public async Task HandleOpen_WhenPriceIsExplicitZero_PreservesZeroPrice()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var productStall = CreateActiveProductStall(stall.Id, null, 45_000m);
        var handler = new OpenInventorySessionHandler(
            new FakeCommandRepository(),
            new FakeReferenceQuery(stall, productStall),
            new FakeUnitOfWork(),
            new FixedBusinessClock(Now, new DateOnly(2026, 8, 15)));

        var result = await handler.Handle(
            new OpenInventorySessionCommand(
                stall.Id,
                ownerId,
                null,
                [new InventoryListingInput(productStall.Id, 1m, 0m)]),
            CancellationToken.None);

        Assert.Equal(0m, Assert.Single(result.Listings).PublicUnitPrice);
    }

    [Fact]
    public async Task HandleOpen_WhenActorDoesNotOwnStall_ThrowsForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var handler = CreateOpenHandler(stall, new FakeCommandRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<InventoryForbiddenException>(() => handler.Handle(
            new OpenInventorySessionCommand(stall.Id, Guid.NewGuid(), null, []),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleOpen_WhenStallIsInactive_ThrowsNotFoundException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        stall.Status = StallStatus.PENDING;
        var handler = CreateOpenHandler(stall, new FakeCommandRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<InventoryNotFoundException>(() => handler.Handle(
            new OpenInventorySessionCommand(stall.Id, ownerId, null, []),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleOpen_WhenProductStallDoesNotBelongToStall_ThrowsNotFoundException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var productStall = CreateActiveProductStall(Guid.NewGuid(), "Foreign", 10m);
        var handler = new OpenInventorySessionHandler(
            new FakeCommandRepository(),
            new FakeReferenceQuery(stall, productStall),
            new FakeUnitOfWork(),
            new FixedBusinessClock(Now, new DateOnly(2026, 8, 15)));

        await Assert.ThrowsAsync<InventoryNotFoundException>(() => handler.Handle(
            new OpenInventorySessionCommand(
                stall.Id,
                ownerId,
                null,
                [new InventoryListingInput(productStall.Id, 1m, null)]),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleOpen_WhenProductStallIsInactive_ThrowsNotFoundException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var productStall = CreateActiveProductStall(stall.Id, "Inactive", 10m);
        productStall.IsActive = false;
        var handler = new OpenInventorySessionHandler(
            new FakeCommandRepository(),
            new FakeReferenceQuery(stall, productStall),
            new FakeUnitOfWork(),
            new FixedBusinessClock(Now, new DateOnly(2026, 8, 15)));

        await Assert.ThrowsAsync<InventoryNotFoundException>(() => handler.Handle(
            new OpenInventorySessionCommand(
                stall.Id,
                ownerId,
                null,
                [new InventoryListingInput(productStall.Id, 1m, null)]),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleOpen_WhenBusinessDateAlreadyExists_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var existing = InventorySession.Open(stall.Id, new DateOnly(2026, 8, 15), Now, ownerId, null);
        var repository = new FakeCommandRepository { ExistingSession = existing };
        var handler = CreateOpenHandler(stall, repository, new FakeUnitOfWork());

        await Assert.ThrowsAsync<InventoryConflictException>(() => handler.Handle(
            new OpenInventorySessionCommand(stall.Id, ownerId, null, []),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleGetCurrent_WhenOwnedSessionIsClosed_ReturnsTodaysSession()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, new DateOnly(2026, 8, 15), Now, ownerId, null);
        session.Close(ownerId, Now.AddHours(8));
        var handler = new GetCurrentInventorySessionHandler(
            new FakeInventoryQuery { Current = session },
            new FakeReferenceQuery(stall),
            new FixedBusinessClock(Now, new DateOnly(2026, 8, 15)));

        var result = await handler.Handle(
            new GetCurrentInventorySessionQuery(stall.Id, ownerId),
            CancellationToken.None);

        Assert.Equal(session.Id, result.Id);
        Assert.Equal(InventorySessionStatus.CLOSED, result.Status);
    }

    [Fact]
    public async Task HandleGetPrevious_WhenSessionDoesNotExist_ThrowsNotFoundException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var handler = new GetPreviousInventorySessionHandler(
            new FakeInventoryQuery(),
            new FakeReferenceQuery(stall),
            new FixedBusinessClock(Now, new DateOnly(2026, 8, 15)));

        await Assert.ThrowsAsync<InventoryNotFoundException>(() => handler.Handle(
            new GetPreviousInventorySessionQuery(stall.Id, ownerId),
            CancellationToken.None));
    }

    private static OpenInventorySessionHandler CreateOpenHandler(
        Stall stall,
        FakeCommandRepository repository,
        FakeUnitOfWork unitOfWork)
        => new(
            repository,
            new FakeReferenceQuery(stall),
            unitOfWork,
            new FixedBusinessClock(Now, new DateOnly(2026, 8, 15)));

    private static Stall CreateActiveStall(Guid ownerId)
        => new() { VendorId = ownerId, Status = StallStatus.ACTIVE };

    private static ProductStall CreateActiveProductStall(
        Guid stallId,
        string? displayName,
        decimal defaultUnitPrice)
        => new()
        {
            StallId = stallId,
            DisplayName = displayName,
            SellingUnit = ProductUnit.KG,
            DefaultUnitPrice = defaultUnitPrice,
            IsActive = true,
            Product = new Product { Name = "Tomato" }
        };

    private sealed class FixedBusinessClock(DateTimeOffset now, DateOnly businessDate) : IBusinessClock
    {
        public DateTimeOffset GetNow() => now;

        public DateOnly GetBusinessDate() => businessDate;
    }

    private sealed class FakeCommandRepository : IInventoryCommandRepository
    {
        public InventorySession? ExistingSession { get; init; }
        public InventorySession? AddedSession { get; private set; }
        public List<DailyProductListing> AddedListings { get; } = [];
        public bool Saved { get; private set; }

        public Task<InventorySession?> FindSessionAsync(
            Guid stallId,
            DateOnly businessDate,
            CancellationToken cancellationToken)
            => Task.FromResult(ExistingSession);

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
        {
            AddedSession = session;
            return Task.CompletedTask;
        }

        public Task AddListingAsync(DailyProductListing listing, CancellationToken cancellationToken)
        {
            AddedListings.Add(listing);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReferenceQuery(
        Stall? stall,
        ProductStall? productStall = null) : IInventoryReferenceQuery
    {
        public Task<Stall?> FindActiveStallAsync(Guid stallId, CancellationToken cancellationToken)
            => Task.FromResult(stall?.Id == stallId ? stall : null);

        public Task<ProductStall?> FindActiveProductStallAsync(
            Guid stallId,
            Guid productStallId,
            CancellationToken cancellationToken)
            => Task.FromResult(productStall?.Id == productStallId ? productStall : null);
    }

    private sealed class FakeUnitOfWork : IInventoryUnitOfWork
    {
        public int ExecutionCount { get; private set; }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return await operation(cancellationToken);
        }
    }

    private sealed class FakeInventoryQuery : IInventoryQuery
    {
        public InventorySession? Current { get; init; }
        public InventorySession? Previous { get; init; }

        public Task<InventorySession?> GetCurrentSessionAsync(
            Guid stallId,
            DateOnly businessDate,
            CancellationToken cancellationToken)
            => Task.FromResult(Current);

        public Task<InventorySession?> GetPreviousSessionAsync(
            Guid stallId,
            DateOnly businessDate,
            CancellationToken cancellationToken)
            => Task.FromResult(Previous);

        public Task<PagedResult<InventoryLedger>> GetLedgerAsync(
            InventoryLedgerListFilter filter,
            CancellationToken cancellationToken)
            => Task.FromResult(new PagedResult<InventoryLedger>([], filter.Page, filter.PageSize, 0));
    }
}
