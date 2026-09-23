using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Finance.Exceptions;
using Haggly.Application.Modules.Finance.Reports;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Finance.Reports;

public sealed class GetVendorRevenueReportHandlerTests
{
    private readonly IRevenueReportQuery _reports = Substitute.For<IRevenueReportQuery>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_OmittedFilters_UsesCurrentUtcDayAndAllChannels()
    {
        // Arrange
        _clock.GetNow().Returns(Now);
        var response = EmptyVendorResponse();
        _reports.GetVendorReportAsync(
                VendorId,
                Arg.Any<VendorRevenueReportRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(response);
        var query = new GetVendorRevenueReportQuery(VendorId, null, null, null, null);

        // Act
        var result = await CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        Assert.Same(response, result);
        await _reports.Received(1).GetVendorReportAsync(
            VendorId,
            Arg.Is<VendorRevenueReportRequest>(request =>
                request.From == new DateTimeOffset(2026, 9, 4, 0, 0, 0, TimeSpan.Zero)
                && request.To == Now
                && request.SaleChannel == SaleChannel.ALL
                && request.StallId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExplicitOwnedStall_NormalizesUtcAndForwardsFilters()
    {
        // Arrange
        var from = new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(7));
        var to = new DateTimeOffset(2026, 9, 2, 7, 0, 0, TimeSpan.FromHours(7));
        _clock.GetNow().Returns(Now);
        _reports.IsStallOwnedByVendorAsync(StallId, VendorId, Arg.Any<CancellationToken>())
            .Returns(true);
        _reports.GetVendorReportAsync(
                VendorId,
                Arg.Any<VendorRevenueReportRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptyVendorResponse());
        var query = new GetVendorRevenueReportQuery(
            VendorId, from, to, SaleChannel.ONLINE, StallId);

        // Act
        await CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        await _reports.Received(1).IsStallOwnedByVendorAsync(
            StallId, VendorId, Arg.Any<CancellationToken>());
        await _reports.Received(1).GetVendorReportAsync(
            VendorId,
            Arg.Is<VendorRevenueReportRequest>(request =>
                request.From == from.ToUniversalTime()
                && request.To == to.ToUniversalTime()
                && request.SaleChannel == SaleChannel.ONLINE
                && request.StallId == StallId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StallNotOwnedByVendor_ThrowsNotFoundWithoutLoadingReport()
    {
        // Arrange
        _clock.GetNow().Returns(Now);
        _reports.IsStallOwnedByVendorAsync(StallId, VendorId, Arg.Any<CancellationToken>())
            .Returns(false);
        var query = new GetVendorRevenueReportQuery(
            VendorId, null, null, SaleChannel.ALL, StallId);

        // Act
        var action = () => CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RevenueReportNotFoundException>(action);
        await _reports.DidNotReceive().GetVendorReportAsync(
            Arg.Any<Guid>(),
            Arg.Any<VendorRevenueReportRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyVendorId_ThrowsValidationWithoutQuerying()
    {
        // Arrange
        var query = new GetVendorRevenueReportQuery(Guid.Empty, null, null, null, null);

        // Act
        var action = () => CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RevenueReportValidationException>(action);
        await _reports.DidNotReceive().GetVendorReportAsync(
            Arg.Any<Guid>(),
            Arg.Any<VendorRevenueReportRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StartAfterEnd_ThrowsValidationWithoutQuerying()
    {
        // Arrange
        _clock.GetNow().Returns(Now);
        var query = new GetVendorRevenueReportQuery(
            VendorId, Now, Now.AddMinutes(-1), SaleChannel.POS, null);

        // Act
        var action = () => CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RevenueReportValidationException>(action);
        await _reports.DidNotReceive().GetVendorReportAsync(
            Arg.Any<Guid>(),
            Arg.Any<VendorRevenueReportRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RangeExceeds366Days_ThrowsValidationWithoutQuerying()
    {
        // Arrange
        _clock.GetNow().Returns(Now);
        var query = new GetVendorRevenueReportQuery(
            VendorId, Now.AddDays(-367), Now, SaleChannel.POS, null);

        // Act
        var action = () => CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RevenueReportValidationException>(action);
        await _reports.DidNotReceive().GetVendorReportAsync(
            Arg.Any<Guid>(),
            Arg.Any<VendorRevenueReportRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UndefinedSaleChannel_ThrowsValidationWithoutQuerying()
    {
        // Arrange
        _clock.GetNow().Returns(Now);
        var query = new GetVendorRevenueReportQuery(
            VendorId, null, null, (SaleChannel)999, null);

        // Act
        var action = () => CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RevenueReportValidationException>(action);
        await _reports.DidNotReceive().GetVendorReportAsync(
            Arg.Any<Guid>(),
            Arg.Any<VendorRevenueReportRequest>(),
            Arg.Any<CancellationToken>());
    }

    private GetVendorRevenueReportHandler CreateSubject() => new(_reports, _clock);

    private static VendorRevenueReportResponse EmptyVendorResponse() => new(0, 0m, []);

    private static readonly Guid VendorId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    private static readonly Guid StallId = Guid.Parse("a1000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 8, 30, 0, TimeSpan.Zero);
}

public sealed class GetAdminRevenueReportHandlerTests
{
    private readonly IRevenueReportQuery _reports = Substitute.For<IRevenueReportQuery>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_ExplicitFilters_ForwardsNormalizedRequest()
    {
        // Arrange
        var from = new DateTimeOffset(2026, 8, 1, 7, 0, 0, TimeSpan.FromHours(7));
        var to = new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(7));
        _clock.GetNow().Returns(Now);
        var response = new AdminRevenueReportResponse(0, 0m, []);
        _reports.GetAdminReportAsync(
                Arg.Any<AdminRevenueReportRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(response);
        var query = new GetAdminRevenueReportQuery(
            from, to, SaleChannel.POS, MarketId, VendorId, StallId);

        // Act
        var result = await CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        Assert.Same(response, result);
        await _reports.Received(1).GetAdminReportAsync(
            Arg.Is<AdminRevenueReportRequest>(request =>
                request.From == from.ToUniversalTime()
                && request.To == to.ToUniversalTime()
                && request.SaleChannel == SaleChannel.POS
                && request.MarketId == MarketId
                && request.VendorId == VendorId
                && request.StallId == StallId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyOptionalId_ThrowsValidationWithoutQuerying()
    {
        // Arrange
        _clock.GetNow().Returns(Now);
        var query = new GetAdminRevenueReportQuery(
            null, null, null, Guid.Empty, null, null);

        // Act
        var action = () => CreateSubject().Handle(query, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RevenueReportValidationException>(action);
        await _reports.DidNotReceive().GetAdminReportAsync(
            Arg.Any<AdminRevenueReportRequest>(),
            Arg.Any<CancellationToken>());
    }

    private GetAdminRevenueReportHandler CreateSubject() => new(_reports, _clock);

    private static readonly Guid MarketId = Guid.Parse("a2000000-0000-0000-0000-000000000001");
    private static readonly Guid VendorId = Guid.Parse("a2000000-0000-0000-0000-000000000002");
    private static readonly Guid StallId = Guid.Parse("a2000000-0000-0000-0000-000000000003");
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 8, 30, 0, TimeSpan.Zero);
}
