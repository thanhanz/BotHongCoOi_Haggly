using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Administration;
using Haggly.Application.Modules.Identity.Administration.Commands;
using Haggly.Domain.Modules.Identity;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Identity.Administration.SuspendVendor;

public sealed class SuspendVendorHandlerTests
{
    private readonly IVendorAdminCommandRepository _repository = Substitute.For<IVendorAdminCommandRepository>();
    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();

    [Fact]
    public async Task Handle_ApprovedVendor_SuspendsVendorAtClockTimeAndSaves()
    {
        // Arrange
        var aggregate = CreateAggregate(ApprovalStatus.APPROVED, UserStatus.ACTIVE);
        _repository.FindByIdAsync(aggregate.User.Id, Arg.Any<CancellationToken>()).Returns(aggregate);
        _clock.GetUtcNow().Returns(DecisionAt);

        // Act
        var result = await CreateSubject().Handle(
            new SuspendVendorCommand(aggregate.User.Id, AdminId), CancellationToken.None);

        // Assert
        Assert.Equal(ApprovalStatus.SUSPENDED, result.ApprovalStatus);
        Assert.Equal(UserStatus.SUSPENDED, result.UserStatus);
        Assert.Equal(DecisionAt, aggregate.VendorProfile.UpdatedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PendingVendor_ThrowsConflictWithoutSaving()
    {
        // Arrange
        var aggregate = CreateAggregate(ApprovalStatus.PENDING, UserStatus.PENDING);
        _repository.FindByIdAsync(aggregate.User.Id, Arg.Any<CancellationToken>()).Returns(aggregate);
        _clock.GetUtcNow().Returns(DecisionAt);

        // Act
        var action = () => CreateSubject().Handle(
            new SuspendVendorCommand(aggregate.User.Id, AdminId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<VendorTransitionConflictException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownVendor_ThrowsNotFoundWithoutSaving()
    {
        // Arrange
        _repository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VendorAdminAggregate?)null);

        // Act
        var action = () => CreateSubject().Handle(
            new SuspendVendorCommand(VendorId, AdminId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<VendorNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private SuspendVendorHandler CreateSubject() => new(_repository, _clock);

    private static VendorAdminAggregate CreateAggregate(ApprovalStatus approval, UserStatus status)
    {
        var user = new User { Status = status, Email = "vendor@example.com", FullName = "Vendor" };
        return new VendorAdminAggregate(user, new VendorProfile
        {
            UserId = user.Id, BusinessName = "Vendor Stall", ApprovalStatus = approval
        });
    }

    private static readonly Guid VendorId = Guid.Parse("73000000-0000-0000-0000-000000000001");
    private static readonly Guid AdminId = Guid.Parse("73000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset DecisionAt = new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
}
