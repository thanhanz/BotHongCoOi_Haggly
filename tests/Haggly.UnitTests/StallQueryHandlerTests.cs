using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Handlers.Stalls;
using Haggly.Application.Modules.Markets.Queries.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Domain.Modules.Markets;
using Xunit;

namespace Haggly.UnitTests;

public sealed class StallQueryHandlerTests
{
    [Fact]
    public async Task HandleGetAll_WhenStallsExist_ReturnsStallDtos()
    {
        var stall = new Stall { Code = "S-001", Name = "Fresh Stall" };
        var handler = new GetStallsHandler(new FakeStallQuery([stall]));

        var result = await handler.Handle(new GetStallsQuery(), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(stall.Id, item.Id);
        Assert.Equal("S-001", item.Code);
    }

    [Fact]
    public async Task HandleGetById_WhenStallExists_ReturnsStallDto()
    {
        var stall = new Stall { Code = "S-001", Name = "Fresh Stall" };
        var handler = new GetStallByIdHandler(new FakeStallQuery([], stall));

        var result = await handler.Handle(
            new GetStallByIdQuery(stall.Id),
            CancellationToken.None);

        Assert.Equal(stall.Id, result.Id);
        Assert.Equal("Fresh Stall", result.Name);
    }

    [Fact]
    public async Task HandleGetById_WhenStallDoesNotExist_ThrowsNotFoundException()
    {
        var handler = new GetStallByIdHandler(new FakeStallQuery());

        await Assert.ThrowsAsync<StallNotFoundException>(() =>
            handler.Handle(new GetStallByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeStallQuery(
        IReadOnlyCollection<Stall>? stalls = null,
        Stall? stall = null) : IStallQuery
    {
        public Task<IReadOnlyCollection<Stall>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(stalls ?? []);

        public Task<Stall?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(stall?.Id == id ? stall : null);
    }
}
