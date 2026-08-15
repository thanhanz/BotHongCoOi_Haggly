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

public sealed class InventoryListingAndAdjustmentHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 15, 2, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAdd_WhenCurrentSessionAndProductAreValid_CreatesListingWithSnapshots()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        var productStall = CreateActiveProductStall(stall.Id, "Fresh Tomato", 45_000m);
        var repository = new FakeCommandRepository { CurrentSession = session };
        var handler = CreateAddHandler(stall, productStall, repository);

        var result = await handler.Handle(
            new AddDailyProductListingCommand(
                stall.Id,
                ownerId,
                new InventoryListingInput(productStall.Id, 25.5m, null)),
            CancellationToken.None);

        var listing = result;
        Assert.Equal(productStall.Id, listing.ProductStallId);
        Assert.Equal("Fresh Tomato", listing.ProductNameSnapshot);
        Assert.Equal(ProductUnit.KG, listing.SellingUnitSnapshot);
        Assert.Equal(45_000m, listing.PublicUnitPrice);
        Assert.Equal(25.5m, listing.CurrentQuantity);
        Assert.Single(repository.AddedListings);
        Assert.True(repository.Saved);
        Assert.Equal(1, repository.TransactionCount);
    }

    [Fact]
    public async Task HandleAdd_WhenListingAlreadyExists_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        var productStall = CreateActiveProductStall(stall.Id, "Tomato", 10m);
        var existing = DailyProductListing.Open(
            session.Id,
            productStall.Id,
            "Tomato",
            ProductUnit.KG,
            10m,
            1m,
            ownerId,
            Now);
        session.DailyProductListings.Add(existing);
        var repository = new FakeCommandRepository
        {
            CurrentSession = session,
            ListingAlreadyExists = true
        };
        var handler = CreateAddHandler(stall, productStall, repository);

        await Assert.ThrowsAsync<InventoryConflictException>(() => handler.Handle(
            new AddDailyProductListingCommand(
                stall.Id,
                ownerId,
                new InventoryListingInput(productStall.Id, 1m, null)),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAdd_WhenSessionIsClosed_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        session.Close(ownerId, Now.AddHours(8));
        var productStall = CreateActiveProductStall(stall.Id, "Tomato", 10m);
        var handler = CreateAddHandler(
            stall,
            productStall,
            new FakeCommandRepository { CurrentSession = session });

        await Assert.ThrowsAsync<InventoryConflictException>(() => handler.Handle(
            new AddDailyProductListingCommand(
                stall.Id,
                ownerId,
                new InventoryListingInput(productStall.Id, 1m, null)),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleUpdate_WhenPriceAndVisibilityChange_UpdatesListingAndCreatesPriceLedger()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        var listing = CreateListing(session, ownerId, 10m, 45_000m);
        var repository = new FakeCommandRepository { Listing = listing };
        var handler = CreateUpdateHandler(stall, repository);

        var result = await handler.Handle(
            new UpdateDailyProductListingCommand(
                stall.Id,
                listing.Id,
                ownerId,
                50_000m,
                DailyListingStatus.HIDDEN,
                0L),
            CancellationToken.None);

        Assert.Equal(50_000m, result.PublicUnitPrice);
        Assert.Equal(DailyListingStatus.HIDDEN, result.Status);
        Assert.Equal(2L, result.Version);
        Assert.Equal(ownerId, listing.UpdatedBy);
        Assert.Equal(Now, listing.UpdatedAt);
        var priceLedger = Assert.Single(
            listing.InventoryLedgers,
            x => x.TransactionType == InventoryTransactionType.PRICE_CHANGE);
        Assert.Equal(45_000m, priceLedger.UnitPriceBefore);
        Assert.Equal(50_000m, priceLedger.UnitPriceAfter);
        Assert.True(repository.Saved);
        Assert.Equal(1, repository.TransactionCount);
    }

    [Fact]
    public async Task HandleUpdate_WhenExpectedVersionIsStale_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        var listing = CreateListing(session, ownerId, 10m, 45_000m);
        listing.ChangePrice(46_000m, ownerId, Now);
        var handler = CreateUpdateHandler(stall, new FakeCommandRepository { Listing = listing });

        await Assert.ThrowsAsync<InventoryConflictException>(() => handler.Handle(
            new UpdateDailyProductListingCommand(
                stall.Id,
                listing.Id,
                ownerId,
                50_000m,
                null,
                0L),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAdjust_WhenDeltaIsValid_UpdatesQuantityAndCreatesAdjustmentLedger()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        var listing = CreateListing(session, ownerId, 10m, 45_000m);
        var repository = new FakeCommandRepository { Listing = listing };
        var handler = CreateAdjustHandler(stall, repository);

        var result = await handler.Handle(
            new AdjustInventoryCommand(
                stall.Id,
                listing.Id,
                ownerId,
                -2.5m,
                "Damaged stock",
                0L),
            CancellationToken.None);

        Assert.Equal(7.5m, result.CurrentQuantity);
        Assert.Equal(7.5m, result.AvailableQuantity);
        Assert.Equal(1L, result.Version);
        var ledger = Assert.Single(
            listing.InventoryLedgers,
            x => x.TransactionType == InventoryTransactionType.ADJUSTMENT);
        Assert.Equal(-2.5m, ledger.QuantityDelta);
        Assert.Equal(10m, ledger.QuantityBefore);
        Assert.Equal(7.5m, ledger.QuantityAfter);
        Assert.Equal("Damaged stock", ledger.Reason);
        Assert.Equal(ownerId, ledger.PerformedBy);
        Assert.True(repository.Saved);
        Assert.Equal(1, repository.TransactionCount);
    }

    [Fact]
    public async Task HandleAdjust_WhenDeltaWouldGoBelowReserved_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        var listing = CreateListing(session, ownerId, 10m, 45_000m);
        listing.UpdateReservedQuantity(5m);
        var repository = new FakeCommandRepository { Listing = listing };
        var handler = CreateAdjustHandler(stall, repository);

        await Assert.ThrowsAsync<InventoryConflictException>(() => handler.Handle(
            new AdjustInventoryCommand(
                stall.Id,
                listing.Id,
                ownerId,
                -6m,
                "Spoilage",
                0L),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAdjust_WhenSessionIsClosed_ThrowsConflictException()
    {
        var ownerId = Guid.NewGuid();
        var stall = CreateActiveStall(ownerId);
        var session = InventorySession.Open(stall.Id, BusinessDate, Now, ownerId, null);
        session.Close(ownerId, Now.AddHours(8));
        var listing = CreateListing(session, ownerId, 10m, 45_000m);
        var handler = CreateAdjustHandler(stall, new FakeCommandRepository { Listing = listing });

        await Assert.ThrowsAsync<InventoryConflictException>(() => handler.Handle(
            new AdjustInventoryCommand(
                stall.Id,
                listing.Id,
                ownerId,
                1m,
                "New stock",
                0L),
            CancellationToken.None));
    }

    private static readonly DateOnly BusinessDate = new(2026, 8, 15);

    private static AddDailyProductListingHandler CreateAddHandler(
        Stall stall,
        ProductStall productStall,
        FakeCommandRepository repository)
        => new(
            repository,
            new FakeReferenceQuery(stall, productStall),
            repository,
            new FixedBusinessClock(Now, BusinessDate));

    private static UpdateDailyProductListingHandler CreateUpdateHandler(
        Stall stall,
        FakeCommandRepository repository)
        => new(
            repository,
            new FakeReferenceQuery(stall),
            repository,
            new FixedBusinessClock(Now, BusinessDate));

    private static AdjustInventoryHandler CreateAdjustHandler(
        Stall stall,
        FakeCommandRepository repository)
        => new(
            repository,
            new FakeReferenceQuery(stall),
            repository,
            new FixedBusinessClock(Now, BusinessDate));

    private static DailyProductListing CreateListing(
        InventorySession session,
        Guid ownerId,
        decimal quantity,
        decimal price)
    {
        var listing = DailyProductListing.Open(
            session.Id,
            Guid.NewGuid(),
            "Tomato",
            ProductUnit.KG,
            price,
            quantity,
            ownerId,
            Now);
        listing.InventorySession = session;
        session.DailyProductListings.Add(listing);
        return listing;
    }

    private static Stall CreateActiveStall(Guid ownerId)
        => new() { VendorId = ownerId, Status = StallStatus.ACTIVE };

    private static ProductStall CreateActiveProductStall(
        Guid stallId,
        string displayName,
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

    private sealed class FakeCommandRepository :
        IInventoryCommandRepository,
        IInventoryUnitOfWork
    {
        public InventorySession? CurrentSession { get; init; }
        public DailyProductListing? Listing { get; init; }
        public bool ListingAlreadyExists { get; init; }
        public List<DailyProductListing> AddedListings { get; } = [];
        public bool Saved { get; private set; }
        public int TransactionCount { get; private set; }

        public Task<InventorySession?> FindSessionAsync(
            Guid stallId,
            DateOnly businessDate,
            CancellationToken cancellationToken)
            => Task.FromResult(CurrentSession?.StallId == stallId
                && CurrentSession.BusinessDate == businessDate
                ? CurrentSession
                : null);

        public Task<DailyProductListing?> FindListingAsync(
            Guid stallId,
            Guid listingId,
            CancellationToken cancellationToken)
            => Task.FromResult(Listing?.Id == listingId ? Listing : null);

        public Task<bool> ListingExistsAsync(
            Guid inventorySessionId,
            Guid productStallId,
            CancellationToken cancellationToken)
            => Task.FromResult(ListingAlreadyExists);

        public Task AddSessionAsync(InventorySession session, CancellationToken cancellationToken)
            => Task.CompletedTask;

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

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            TransactionCount++;
            return await operation(cancellationToken);
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
}
