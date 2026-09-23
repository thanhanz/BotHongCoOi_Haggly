using Haggly.Application.Abstractions.Markets;
using Haggly.Application.Modules.Markets.Queries.Markets;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Markets.Queries;

public sealed class GetMarketsHandlerTests
{
    private readonly IMarketQuery _query = Substitute.For<IMarketQuery>();

    [Fact]
    public async Task Handle_ActiveMarketsExist_ReturnsMappedMarkets()
    {
        // Arrange
        var market = new Market { Code = "MKT-001", Name = "Central Market", Address = "1 Main Street" };
        _query.GetAllAsync(Arg.Any<CancellationToken>()).Returns([market]);

        // Act
        var result = await new GetMarketsHandler(_query)
            .Handle(new GetMarketsQuery(), CancellationToken.None);

        // Assert
        var mapped = Assert.Single(result);
        Assert.Equal(market.Id, mapped.Id);
        Assert.Equal("MKT-001", mapped.Code);
        Assert.Equal("Central Market", mapped.Name);
    }
}
