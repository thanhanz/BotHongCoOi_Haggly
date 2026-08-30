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
}
