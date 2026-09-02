using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Markets.Commands.Stalls;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Application.Modules.Markets.CreateStall;

public sealed class CreateStallHandlerTests
{
    private readonly IStallCommandRepository _repository = Substitute.For<IStallCommandRepository>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_ValidStall_CreatesStallAndInventory()
    {
        // Arrange
        var marketId = Guid.Parse("A1000000-0000-0000-0000-000000000001");
        var vendorId = Guid.Parse("A1000000-0000-0000-0000-000000000002");
        var actorId = Guid.Parse("A1000000-0000-0000-0000-000000000003");
        var now = new DateTimeOffset(2026, 8, 30, 18, 0, 0, TimeSpan.Zero);
        _repository.MarketExistsAsync(marketId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.VendorExistsAsync(vendorId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.CodeExistsAsync(marketId, "S-01", null, Arg.Any<CancellationToken>()).Returns(false);
        _clock.GetNow().Returns(now);

        // Act
        var result = await new CreateStallHandler(_repository, _clock).Handle(
            new CreateStallCommand(marketId, vendorId, actorId, " S-01 ", " Main Stall "), CancellationToken.None);

        // Assert
        Assert.Equal("S-01", result.Code);
        await _repository.Received(1).AddAsync(Arg.Is<Stall>(stall => stall.VendorId == vendorId), Arg.Any<CancellationToken>());
        await _repository.Received(1).AddInventoryAsync(Arg.Is<DomainInventory>(inventory => inventory.StallId == result.Id), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
