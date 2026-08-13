using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Markets;
using Haggly.Application.Modules.Markets.Dtos.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Application.Modules.Markets.Handlers.Markets;
using Haggly.Domain.Modules.Markets;
using Xunit;

namespace Haggly.UnitTests;

public sealed class MarketCommandHandlerTests
{
    [Fact]
    public async Task HandleCreate_WhenCommandIsValid_CreatesActiveMarket()
    {
        var repository = new FakeMarketCommandRepository();
        var handler = new CreateMarketHandler(repository);
        var command = new CreateMarketCommand("M-001", "Central Market", "1 Main Street");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("M-001", result.Code);
        Assert.Equal(MarketStatus.ACTIVE, result.Status);
        Assert.Single(repository.Markets);
    }

    [Fact]
    public async Task HandleCreate_WhenCodeAlreadyExists_ThrowsConflict()
    {
        var repository = new FakeMarketCommandRepository();
        repository.Markets.Add(new Market { Code = "M-001" });
        var handler = new CreateMarketHandler(repository);

        var exception = await Assert.ThrowsAsync<MarketConflictException>(() =>
            handler.Handle(
                new CreateMarketCommand("M-001", "Another Market", "Address"),
                CancellationToken.None));

        Assert.Contains("code", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleCreate_WhenRequiredFieldIsMissing_ThrowsValidationException()
    {
        var handler = new CreateMarketHandler(new FakeMarketCommandRepository());

        await Assert.ThrowsAsync<MarketValidationException>(() =>
            handler.Handle(
                new CreateMarketCommand("", "Market", "Address"),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleUpdate_WhenMarketExists_UpdatesFieldsAndStatus()
    {
        var market = new Market { Code = "M-001", Name = "Old", Address = "Old Address" };
        var repository = new FakeMarketCommandRepository { Markets = [market] };
        var handler = new UpdateMarketHandler(repository);

        var result = await handler.Handle(
            new UpdateMarketCommand(
                market.Id,
                "M-002",
                "Updated",
                "New Address",
                null,
                null,
                null,
                null,
                MarketStatus.SUSPENDED),
            CancellationToken.None);

        Assert.Equal("M-002", result.Code);
        Assert.Equal("Updated", market.Name);
        Assert.Equal(MarketStatus.SUSPENDED, market.Status);
    }

    [Fact]
    public async Task HandleUpdate_WhenMarketDoesNotExist_ThrowsNotFoundException()
    {
        var handler = new UpdateMarketHandler(new FakeMarketCommandRepository());

        await Assert.ThrowsAsync<MarketNotFoundException>(() =>
            handler.Handle(
                new UpdateMarketCommand(
                    Guid.NewGuid(), "M-001", "Market", "Address", null, null, null, null, MarketStatus.ACTIVE),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleDelete_WhenMarketExists_SoftDeletesAndSavesMarket()
    {
        var market = new Market { Code = "M-001" };
        var repository = new FakeMarketCommandRepository { Markets = [market] };
        var handler = new DeleteMarketHandler(repository);

        await handler.Handle(new DeleteMarketCommand(market.Id), CancellationToken.None);

        Assert.NotNull(market.DeletedAt);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    private sealed class FakeMarketCommandRepository : IMarketCommandRepository
    {
        public List<Market> Markets { get; set; } = [];
        public int SaveChangesCalls { get; private set; }

        public Task<bool> CodeExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
            => Task.FromResult(Markets.Any(market =>
                market.DeletedAt is null
                && market.Code == code
                && (excludingId is null || market.Id != excludingId)));

        public Task<Market?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Markets.SingleOrDefault(market => market.Id == id));

        public Task AddAsync(Market market, CancellationToken cancellationToken)
        {
            Markets.Add(market);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}
