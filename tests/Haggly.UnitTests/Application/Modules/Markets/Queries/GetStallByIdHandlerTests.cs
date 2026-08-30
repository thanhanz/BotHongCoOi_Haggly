using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Exceptions.Stalls;
using Haggly.Application.Modules.Markets.Queries.Stalls;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Markets.Queries;

public sealed class GetStallByIdHandlerTests
{
    private readonly IStallQuery _query = Substitute.For<IStallQuery>();

    [Fact]
    public async Task Handle_ExistingStall_ReturnsMappedStall()
    {
        // Arrange
        var stall = new Stall { Code = "S-01", Name = "North Stall" };
        _query.GetByIdAsync(stall.Id, Arg.Any<CancellationToken>()).Returns(stall);

        // Act
        var result = await new GetStallByIdHandler(_query).Handle(
            new GetStallByIdQuery(stall.Id), CancellationToken.None);

        // Assert
        Assert.Equal(stall.Id, result.Id);
        Assert.Equal("S-01", result.Code);
    }

    [Fact]
    public async Task Handle_MissingStall_ThrowsNotFound()
    {
        // Arrange
        _query.GetByIdAsync(StallId, Arg.Any<CancellationToken>()).Returns((Stall?)null);

        // Act
        var action = () => new GetStallByIdHandler(_query).Handle(
            new GetStallByIdQuery(StallId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<StallNotFoundException>(action);
    }

    [Fact]
    public async Task Handle_EmptyId_ThrowsValidationWithoutQuerying()
    {
        // Arrange

        // Act
        var action = () => new GetStallByIdHandler(_query).Handle(
            new GetStallByIdQuery(Guid.Empty), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<StallValidationException>(action);
        await _query.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static readonly Guid StallId = Guid.Parse("92000000-0000-0000-0000-000000000001");
}
