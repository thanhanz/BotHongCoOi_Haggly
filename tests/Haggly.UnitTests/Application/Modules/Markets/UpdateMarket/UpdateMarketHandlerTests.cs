using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Markets.UpdateMarket;

public sealed class UpdateMarketHandlerTests
{
    private readonly IMarketCommandRepository _repository = Substitute.For<IMarketCommandRepository>();

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesMarketAndSaves()
    {
        // Arrange
        var market = new Market { Code = "OLD", Name = "Old", Address = "Old Address" };
        _repository.FindByIdAsync(market.Id, Arg.Any<CancellationToken>()).Returns(market);
        _repository.CodeExistsAsync("NEW", market.Id, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await CreateSubject().Handle(
            new UpdateMarketCommand(market.Id, " NEW ", " New Market ", " New Address ", null, null, null, null, MarketStatus.ACTIVE), CancellationToken.None);

        // Assert
        Assert.Equal("NEW", result.Code);
        Assert.Equal("New Market", market.Name);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsConflictWithoutSaving()
    {
        // Arrange
        var market = new Market { Code = "OLD", Name = "Old", Address = "Old Address" };
        _repository.FindByIdAsync(market.Id, Arg.Any<CancellationToken>()).Returns(market);
        _repository.CodeExistsAsync("NEW", market.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateMarketCommand(market.Id, "NEW", "New", "Address", null, null, null, null, MarketStatus.ACTIVE), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<MarketConflictException>(action);
        Assert.Equal("OLD", market.Code);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidCommand_ThrowsValidationWithoutQueryOrSave()
    {
        // Arrange

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateMarketCommand(Guid.Empty, "NEW", "Name", "Address", null, null, null, null, MarketStatus.ACTIVE), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<MarketValidationException>(action);
        await _repository.DidNotReceive().FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private UpdateMarketHandler CreateSubject() => new(_repository);
}
