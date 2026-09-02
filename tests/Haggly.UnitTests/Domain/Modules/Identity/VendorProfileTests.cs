using Haggly.Domain.Modules.Identity;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Identity;

public sealed class VendorProfileTests
{
    [Fact]
    public void Approve_PendingVendor_ActivatesUserAndRecordsApproval()
    {
        // Arrange
        var user = new User { Status = UserStatus.PENDING };
        var vendor = new VendorProfile { UserId = user.Id, ApprovalStatus = ApprovalStatus.PENDING };
        var approvedBy = Guid.Parse("50000000-0000-0000-0000-000000000002");
        var decidedAt = new DateTimeOffset(2026, 8, 12, 10, 30, 0, TimeSpan.Zero);

        // Act
        vendor.Approve(user, approvedBy, decidedAt);

        // Assert
        Assert.Equal(ApprovalStatus.APPROVED, vendor.ApprovalStatus);
        Assert.Equal(UserStatus.ACTIVE, user.Status);
        Assert.Equal(decidedAt, vendor.ApprovedAt);
        Assert.Equal(approvedBy, vendor.ApprovedBy);
    }

    [Fact]
    public void Approve_RejectedVendor_RejectsInvalidTransition()
    {
        // Arrange
        var user = new User { Status = UserStatus.PENDING };
        var vendor = new VendorProfile { UserId = user.Id, ApprovalStatus = ApprovalStatus.REJECTED };

        // Act
        var action = () => vendor.Approve(
            user,
            Guid.Parse("50000000-0000-0000-0000-000000000002"),
            new DateTimeOffset(2026, 8, 12, 10, 30, 0, TimeSpan.Zero));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void Reject_PendingVendor_SuspendsUserAndRejectsVendor()
    {
        // Arrange
        var user = new User { Status = UserStatus.PENDING };
        var vendor = new VendorProfile { UserId = user.Id, ApprovalStatus = ApprovalStatus.PENDING };
        var actorId = Guid.Parse("50000000-0000-0000-0000-000000000002");
        var decidedAt = new DateTimeOffset(2026, 8, 12, 10, 30, 0, TimeSpan.Zero);

        // Act
        vendor.Reject(user, actorId, decidedAt);

        // Assert
        Assert.Equal(ApprovalStatus.REJECTED, vendor.ApprovalStatus);
        Assert.Equal(UserStatus.SUSPENDED, user.Status);
        Assert.Null(vendor.ApprovedAt);
        Assert.Null(vendor.ApprovedBy);
    }

    [Fact]
    public void Suspend_ApprovedVendor_SuspendsUserAndVendor()
    {
        // Arrange
        var user = new User { Status = UserStatus.ACTIVE };
        var vendor = new VendorProfile { UserId = user.Id, ApprovalStatus = ApprovalStatus.APPROVED };
        var actorId = Guid.Parse("50000000-0000-0000-0000-000000000002");
        var decidedAt = new DateTimeOffset(2026, 8, 12, 10, 30, 0, TimeSpan.Zero);

        // Act
        vendor.Suspend(user, actorId, decidedAt);

        // Assert
        Assert.Equal(ApprovalStatus.SUSPENDED, vendor.ApprovalStatus);
        Assert.Equal(UserStatus.SUSPENDED, user.Status);
        Assert.Equal(actorId, vendor.UpdatedBy);
        Assert.Equal(decidedAt, vendor.UpdatedAt);
    }

    [Theory]
    [InlineData(ApprovalStatus.APPROVED, true)]
    [InlineData(ApprovalStatus.PENDING, false)]
    public void Decision_InvalidState_RejectsTransition(ApprovalStatus status, bool reject)
    {
        // Arrange
        var user = new User { Status = UserStatus.ACTIVE };
        var vendor = new VendorProfile { UserId = user.Id, ApprovalStatus = status };
        var actorId = Guid.Parse("50000000-0000-0000-0000-000000000002");
        var decidedAt = new DateTimeOffset(2026, 8, 12, 10, 30, 0, TimeSpan.Zero);

        // Act
        Action action = reject
            ? () => vendor.Reject(user, actorId, decidedAt)
            : () => vendor.Suspend(user, actorId, decidedAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(status, vendor.ApprovalStatus);
    }
}
