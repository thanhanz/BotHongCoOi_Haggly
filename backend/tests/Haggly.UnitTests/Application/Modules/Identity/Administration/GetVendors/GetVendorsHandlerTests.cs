using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Common;
using Haggly.Application.Modules.Identity.Administration;
using Haggly.Application.Modules.Identity.Administration.Queries;
using Haggly.Application.Modules.Identity.Dtos;
using Haggly.Domain.Modules.Identity;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Identity.Administration.GetVendors;

public sealed class GetVendorsHandlerTests
{
    private readonly IVendorAdminQuery _query = Substitute.For<IVendorAdminQuery>();

    [Fact]
    public async Task Handle_DefaultFilter_ForwardsDefaultPagingAndReturnsResult()
    {
        // Arrange
        var expected = new PagedResult<VendorQueryDto>([], 1, 20, 0);
        _query.GetPageAsync(Arg.Any<VendorListFilter>(), Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        var result = await CreateSubject().Handle(new GetVendorsQuery(), CancellationToken.None);

        // Assert
        Assert.Same(expected, result);
        await _query.Received(1).GetPageAsync(
            Arg.Is<VendorListFilter>(filter => filter.Page == 1 && filter.PageSize == 20 && filter.Search == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FilterAndSearch_ForwardsNormalizedValues()
    {
        // Arrange
        _query.GetPageAsync(Arg.Any<VendorListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<VendorQueryDto>([], 2, 50, 0));

        // Act
        await CreateSubject().Handle(
            new GetVendorsQuery(ApprovalStatus.APPROVED, "  fresh  ", 2, 50), CancellationToken.None);

        // Assert
        await _query.Received(1).GetPageAsync(
            Arg.Is<VendorListFilter>(filter =>
                filter.ApprovalStatus == ApprovalStatus.APPROVED && filter.Search == "fresh" &&
                filter.Page == 2 && filter.PageSize == 50),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Handle_InvalidPaging_ThrowsValidationWithoutQuery(int page, int pageSize)
    {
        // Arrange

        // Act
        var action = () => CreateSubject().Handle(
            new GetVendorsQuery(null, null, page, pageSize), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<VendorQueryValidationException>(action);
        await _query.DidNotReceive().GetPageAsync(
            Arg.Any<VendorListFilter>(), Arg.Any<CancellationToken>());
    }

    private GetVendorsHandler CreateSubject() => new(_query);
}
