using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Administration;
using Haggly.Application.Modules.Identity.Administration.Commands;
using Haggly.Domain.Modules.Identity;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Identity.Administration.RejectVendor;

public sealed class RejectVendorHandlerTests
{
    private readonly IVendorAdminCommandRepository _repository = Substitute.For<IVendorAdminCommandRepository>();
    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();

    [Fact]
    public async Task Handle_PendingVendor_RejectsVendorAtClockTimeAndSaves()
    {
        // Arrange
        var aggregate = CreateAggregate(ApprovalStatus.PENDING, UserStatus.PENDING);
        _repository.FindByIdAsync(aggregate.User.Id, Arg.Any<CancellationToken>()).Returns(aggregate);
        _clock.GetUtcNow().Returns(DecisionAt);

        // Act
        var result = await CreateSubject().Handle(
            new RejectVendorCommand(aggregate.User.Id, AdminId), CancellationToken.None);

        // Assert
        Assert.Equal(ApprovalStatus.REJECTED, result.ApprovalStatus);
        Assert.Equal(UserStatus.SUSPENDED, result.UserStatus);
        Assert.Equal(DecisionAt, aggregate.VendorProfile.UpdatedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApprovedVendor_ThrowsConflictWithoutSaving()
    {
        // Arrange
        var aggregate = CreateAggregate(ApprovalStatus.APPROVED, UserStatus.ACTIVE);
        _repository.FindByIdAsync(aggregate.User.Id, Arg.Any<CancellationToken>()).Returns(aggregate);
        _clock.GetUtcNow().Returns(DecisionAt);

        // Act
        var action = () => CreateSubject().Handle(
            new RejectVendorCommand(aggregate.User.Id, AdminId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<VendorTransitionConflictException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_ForwardsTokenToRepository()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _repository.FindByIdAsync(Arg.Any<Guid>(), cancellation.Token)
            .Returns(Task.FromCanceled<VendorAdminAggregate?>(cancellation.Token));

        // Act
        var action = () => CreateSubject().Handle(
            new RejectVendorCommand(VendorId, AdminId), cancellation.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _repository.Received(1).FindByIdAsync(VendorId, cancellation.Token);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private RejectVendorHandler CreateSubject() => new(_repository, _clock);

    private static VendorAdminAggregate CreateAggregate(ApprovalStatus approval, UserStatus status)
    {
        var user = new User { Status = status, Email = "vendor@example.com", FullName = "Vendor" };
        return new VendorAdminAggregate(user, new VendorProfile
        {
            UserId = user.Id, BusinessName = "Vendor Stall", ApprovalStatus = approval
        });
    }

    private static readonly Guid VendorId = Guid.Parse("72000000-0000-0000-0000-000000000001");
    private static readonly Guid AdminId = Guid.Parse("72000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset DecisionAt = new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
}
