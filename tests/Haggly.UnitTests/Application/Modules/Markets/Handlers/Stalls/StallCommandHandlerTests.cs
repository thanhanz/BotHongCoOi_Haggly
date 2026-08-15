using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Stalls;
using Haggly.Application.Modules.Markets.Dtos.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Domain.Modules.Markets;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Markets.Handlers.Stalls;

public sealed class StallCommandHandlerTests
{
    [Fact]
    public async Task HandleCreate_WhenCommandIsValid_CreatesPendingStall()
    {
        var marketId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var repository = new FakeStallCommandRepository
        {
            ExistingMarketIds = [marketId],
            ExistingVendorIds = [vendorId]
        };
        var handler = new CreateStallHandler(repository);

        var result = await handler.Handle(
            new CreateStallCommand(marketId, vendorId, "S-001", "Fresh Stall"),
            CancellationToken.None);

        Assert.Equal("S-001", result.Code);
        Assert.Equal(StallStatus.PENDING, result.Status);
        Assert.Single(repository.Stalls);
    }

    [Fact]
    public async Task HandleCreate_WhenMarketDoesNotExist_ThrowsNotFoundException()
    {
        var handler = new CreateStallHandler(new FakeStallCommandRepository
        {
            ExistingVendorIds = [Guid.NewGuid()]
        });

        await Assert.ThrowsAsync<StallNotFoundException>(() =>
            handler.Handle(
                new CreateStallCommand(Guid.NewGuid(), Guid.NewGuid(), "S-001", "Stall"),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleCreate_WhenVendorDoesNotExist_ThrowsNotFoundException()
    {
        var marketId = Guid.NewGuid();
        var handler = new CreateStallHandler(new FakeStallCommandRepository
        {
            ExistingMarketIds = [marketId]
        });

        await Assert.ThrowsAsync<StallNotFoundException>(() =>
            handler.Handle(
                new CreateStallCommand(marketId, Guid.NewGuid(), "S-001", "Stall"),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleUpdate_WhenStallExists_UpdatesFieldsAndStatus()
    {
        var marketId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var stall = new Stall
        {
            MarketId = marketId,
            VendorId = vendorId,
            Code = "S-001",
            Name = "Old Stall"
        };
        var repository = new FakeStallCommandRepository
        {
            Stalls = [stall],
            ExistingMarketIds = [marketId],
            ExistingVendorIds = [vendorId]
        };
        var handler = new UpdateStallHandler(repository);

        var result = await handler.Handle(
            new UpdateStallCommand(
                stall.Id,
                marketId,
                vendorId,
                "S-002",
                "Updated Stall",
                "Near Gate 1",
                "0123456789",
                StallStatus.ACTIVE),
            CancellationToken.None);

        Assert.Equal("S-002", result.Code);
        Assert.Equal("Updated Stall", stall.Name);
        Assert.Equal(StallStatus.ACTIVE, stall.Status);
    }

    [Fact]
    public async Task HandleDelete_WhenStallExists_SoftDeletesAndSavesStall()
    {
        var stall = new Stall { Code = "S-001" };
        var repository = new FakeStallCommandRepository { Stalls = [stall] };
        var handler = new DeleteStallHandler(repository);

        await handler.Handle(new DeleteStallCommand(stall.Id), CancellationToken.None);

        Assert.NotNull(stall.DeletedAt);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    private sealed class FakeStallCommandRepository : IStallCommandRepository
    {
        public List<Stall> Stalls { get; set; } = [];
        public HashSet<Guid> ExistingMarketIds { get; set; } = [];
        public HashSet<Guid> ExistingVendorIds { get; set; } = [];
        public int SaveChangesCalls { get; private set; }

        public Task<bool> CodeExistsAsync(
            Guid marketId,
            string code,
            Guid? excludingId,
            CancellationToken cancellationToken)
            => Task.FromResult(Stalls.Any(stall =>
                stall.DeletedAt is null
                && stall.MarketId == marketId
                && stall.Code == code
                && (excludingId is null || stall.Id != excludingId)));

        public Task<bool> MarketExistsAsync(Guid marketId, CancellationToken cancellationToken)
            => Task.FromResult(ExistingMarketIds.Contains(marketId));

        public Task<bool> VendorExistsAsync(Guid vendorId, CancellationToken cancellationToken)
            => Task.FromResult(ExistingVendorIds.Contains(vendorId));

        public Task<Stall?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Stalls.SingleOrDefault(stall => stall.Id == id));

        public Task AddAsync(Stall stall, CancellationToken cancellationToken)
        {
            Stalls.Add(stall);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}
