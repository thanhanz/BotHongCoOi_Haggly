using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Markets.UpdateStall;

public sealed class UpdateStallHandlerTests
{
    private readonly IStallCommandRepository _repository = Substitute.For<IStallCommandRepository>();

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesStallAndSaves()
    {
        // Arrange
        var stall = new Stall { MarketId = MarketId, VendorId = VendorId, Code = "OLD", Name = "Old" };
        _repository.FindByIdAsync(stall.Id, Arg.Any<CancellationToken>()).Returns(stall);
        _repository.MarketExistsAsync(MarketId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.VendorExistsAsync(VendorId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.CodeExistsAsync(MarketId, "NEW", stall.Id, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await CreateSubject().Handle(
            new UpdateStallCommand(stall.Id, MarketId, VendorId, " NEW ", " New Stall ", " Location ", " 123 ", StallStatus.ACTIVE), CancellationToken.None);

        // Assert
        Assert.Equal("NEW", result.Code);
        Assert.Equal("New Stall", stall.Name);
        Assert.Equal("Location", stall.LocationDescription);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsConflictWithoutSaving()
    {
        // Arrange
        var stall = new Stall { MarketId = MarketId, VendorId = VendorId, Code = "OLD", Name = "Old" };
        _repository.FindByIdAsync(stall.Id, Arg.Any<CancellationToken>()).Returns(stall);
        _repository.MarketExistsAsync(MarketId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.VendorExistsAsync(VendorId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.CodeExistsAsync(MarketId, "NEW", stall.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateStallCommand(stall.Id, MarketId, VendorId, "NEW", "New", null, null, StallStatus.ACTIVE), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<StallConflictException>(action);
        Assert.Equal("OLD", stall.Code);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidCommand_ThrowsValidationWithoutQueryOrSave()
    {
        // Arrange

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateStallCommand(Guid.Empty, MarketId, VendorId, "NEW", "Name", null, null, StallStatus.ACTIVE), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<StallValidationException>(action);
        await _repository.DidNotReceive().FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private UpdateStallHandler CreateSubject() => new(_repository);
    private static readonly Guid MarketId = Guid.Parse("82000000-0000-0000-0000-000000000001");
    private static readonly Guid VendorId = Guid.Parse("82000000-0000-0000-0000-000000000002");
}
