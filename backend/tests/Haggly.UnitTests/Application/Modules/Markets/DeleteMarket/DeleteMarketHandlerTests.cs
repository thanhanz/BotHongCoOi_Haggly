using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Markets;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Markets.DeleteMarket;

public sealed class DeleteMarketHandlerTests
{
    private readonly IMarketCommandRepository _repository = Substitute.For<IMarketCommandRepository>();

    [Fact]
    public async Task Handle_ExistingMarket_SoftDeletesAndSaves()
    {
        // Arrange
        var market = new Market { Code = "MKT", Name = "Market", Address = "Address" };
        _repository.FindByIdAsync(market.Id, Arg.Any<CancellationToken>()).Returns(market);

        // Act
        var result = await CreateSubject().Handle(new DeleteMarketCommand(market.Id), CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.NotNull(market.DeletedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MarketDoesNotExist_ThrowsNotFoundWithoutSaving()
    {
        // Arrange
        _repository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Market?)null);

        // Act
        var action = () => CreateSubject().Handle(new DeleteMarketCommand(MarketId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<MarketNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyId_ThrowsValidationWithoutQueryOrSave()
    {
        // Arrange

        // Act
        var action = () => CreateSubject().Handle(new DeleteMarketCommand(Guid.Empty), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<MarketValidationException>(action);
        await _repository.DidNotReceive().FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private DeleteMarketHandler CreateSubject() => new(_repository);
    private static readonly Guid MarketId = Guid.Parse("81000000-0000-0000-0000-000000000001");
}
