using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Markets.CreateMarket;

public sealed class CreateMarketHandlerTests
{
    private readonly IMarketCommandRepository _repository = Substitute.For<IMarketCommandRepository>();

    [Fact]
    public async Task Handle_ValidMarket_CreatesAndSavesMarket()
    {
        // Arrange
        _repository.CodeExistsAsync("M-01", null, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await new CreateMarketHandler(_repository).Handle(
            new CreateMarketCommand(" M-01 ", " Fresh Market ", " Main Street "), CancellationToken.None);

        // Assert
        Assert.Equal("M-01", result.Code);
        Assert.Equal("Fresh Market", result.Name);
        await _repository.Received(1).AddAsync(Arg.Is<Market>(market => market.Code == "M-01"), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsConflictWithoutSaving()
    {
        // Arrange
        _repository.CodeExistsAsync("M-01", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var action = () => new CreateMarketHandler(_repository).Handle(
            new CreateMarketCommand("M-01", "Fresh Market", "Main Street"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<MarketConflictException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
