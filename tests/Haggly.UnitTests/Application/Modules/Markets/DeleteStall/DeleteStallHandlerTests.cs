using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Commands.Stalls;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Markets.DeleteStall;

public sealed class DeleteStallHandlerTests
{
    private readonly IStallCommandRepository _repository = Substitute.For<IStallCommandRepository>();

    [Fact]
    public async Task Handle_ExistingStall_SoftDeletesAndSaves()
    {
        // Arrange
        var stall = new Stall { Code = "S-01", Name = "Stall" };
        _repository.FindByIdAsync(stall.Id, Arg.Any<CancellationToken>()).Returns(stall);

        // Act
        var result = await CreateSubject().Handle(new DeleteStallCommand(stall.Id), CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.NotNull(stall.DeletedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StallDoesNotExist_ThrowsNotFoundWithoutSaving()
    {
        // Arrange
        _repository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Stall?)null);

        // Act
        var action = () => CreateSubject().Handle(new DeleteStallCommand(StallId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<StallNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyId_ThrowsValidationWithoutQueryOrSave()
    {
        // Arrange

        // Act
        var action = () => CreateSubject().Handle(new DeleteStallCommand(Guid.Empty), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<StallValidationException>(action);
        await _repository.DidNotReceive().FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private DeleteStallHandler CreateSubject() => new(_repository);
    private static readonly Guid StallId = Guid.Parse("83000000-0000-0000-0000-000000000001");
}
