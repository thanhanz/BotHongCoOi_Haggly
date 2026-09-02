using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Application.Modules.Markets.Queries.Markets;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Markets.Queries;

public sealed class GetMarketByIdHandlerTests
{
    private readonly IMarketQuery _query = Substitute.For<IMarketQuery>();

    [Fact]
    public async Task Handle_ExistingMarket_ReturnsMappedMarket()
    {
        // Arrange
        var market = new Market { Code = "M-01", Name = "Central", Address = "Main" };
        _query.GetByIdAsync(market.Id, Arg.Any<CancellationToken>()).Returns(market);

        // Act
        var result = await CreateSubject().Handle(new GetMarketByIdQuery(market.Id), CancellationToken.None);

        // Assert
        Assert.Equal(market.Id, result.Id);
        Assert.Equal("M-01", result.Code);
        await _query.Received(1).GetByIdAsync(market.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MarketDoesNotExist_ThrowsNotFound()
    {
        // Arrange
        _query.GetByIdAsync(MarketId, Arg.Any<CancellationToken>()).Returns((Market?)null);

        // Act
        var action = () => CreateSubject().Handle(new GetMarketByIdQuery(MarketId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<MarketNotFoundException>(action);
    }

    [Fact]
    public async Task Handle_EmptyId_ThrowsValidationWithoutQuerying()
    {
        // Arrange

        // Act
        var action = () => CreateSubject().Handle(new GetMarketByIdQuery(Guid.Empty), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<MarketValidationException>(action);
        await _query.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private GetMarketByIdHandler CreateSubject() => new(_query);
    private static readonly Guid MarketId = Guid.Parse("91000000-0000-0000-0000-000000000001");
}
