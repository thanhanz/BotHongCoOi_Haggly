using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Dtos.Markets;
using Haggly.Application.Modules.Markets.Handlers.Markets;
using Haggly.Application.Modules.Markets.Queries.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Domain.Modules.Markets;
using Xunit;

namespace Haggly.UnitTests;

public sealed class MarketQueryHandlerTests
{
    [Fact]
    public async Task HandleGetAll_WhenMarketsExist_ReturnsMarketDtos()
    {
        var market = new Market
        {
            Code = "M-001",
            Name = "Central Market",
            Address = "Main Street"
        };
        var handler = new GetMarketsHandler(new FakeMarketQuery([market]));

        var result = await handler.Handle(new GetMarketsQuery(), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(market.Id, item.Id);
        Assert.Equal("M-001", item.Code);
    }

    [Fact]
    public async Task HandleGetById_WhenMarketExists_ReturnsMarketDto()
    {
        var market = new Market { Code = "M-001", Name = "Central Market" };
        var handler = new GetMarketByIdHandler(new FakeMarketQuery([], market));

        var result = await handler.Handle(
            new GetMarketByIdQuery(market.Id),
            CancellationToken.None);

        Assert.Equal(market.Id, result.Id);
        Assert.Equal("Central Market", result.Name);
    }

    [Fact]
    public async Task HandleGetById_WhenMarketDoesNotExist_ThrowsNotFoundException()
    {
        var handler = new GetMarketByIdHandler(new FakeMarketQuery());

        await Assert.ThrowsAsync<MarketNotFoundException>(() =>
            handler.Handle(new GetMarketByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeMarketQuery(
        IReadOnlyCollection<Market>? markets = null,
        Market? market = null) : IMarketQuery
    {
        public Task<IReadOnlyCollection<Market>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(markets ?? []);

        public Task<Market?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(market?.Id == id ? market : null);
    }
}
