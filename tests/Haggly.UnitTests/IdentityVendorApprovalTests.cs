using Haggly.Domain.Modules.Identity;
using Xunit;

namespace Haggly.UnitTests;

public sealed class IdentityVendorApprovalTests
{
    private static readonly DateTimeOffset DecisionTime =
        new(2026, 8, 12, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Approve_PendingVendor_ActivatesUserAndRecordsApproval()
    {
        var user = CreateUser(UserStatus.PENDING);
        var vendor = CreateVendor(user.Id, ApprovalStatus.PENDING);
        var adminId = Guid.NewGuid();

        vendor.Approve(user, adminId, DecisionTime);

        Assert.Equal(ApprovalStatus.APPROVED, vendor.ApprovalStatus);
        Assert.Equal(UserStatus.ACTIVE, user.Status);
        Assert.Equal(DecisionTime, vendor.ApprovedAt);
        Assert.Equal(adminId, vendor.ApprovedBy);
        Assert.Equal(DecisionTime, vendor.UpdatedAt);
        Assert.Equal(adminId, vendor.UpdatedBy);
    }

    [Fact]
    public void Approve_SuspendedVendor_ReinstatesUserAndRefreshesApproval()
    {
        var user = CreateUser(UserStatus.SUSPENDED);
        var vendor = CreateVendor(user.Id, ApprovalStatus.SUSPENDED);
        var adminId = Guid.NewGuid();

        vendor.Approve(user, adminId, DecisionTime);

        Assert.Equal(ApprovalStatus.APPROVED, vendor.ApprovalStatus);
        Assert.Equal(UserStatus.ACTIVE, user.Status);
        Assert.Equal(DecisionTime, vendor.ApprovedAt);
        Assert.Equal(adminId, vendor.ApprovedBy);
    }

    [Fact]
    public void Reject_PendingVendor_SuspendsUserAndClearsApproval()
    {
        var user = CreateUser(UserStatus.PENDING);
        var vendor = CreateVendor(user.Id, ApprovalStatus.PENDING);
        var adminId = Guid.NewGuid();

        vendor.Reject(user, adminId, DecisionTime);

        Assert.Equal(ApprovalStatus.REJECTED, vendor.ApprovalStatus);
        Assert.Equal(UserStatus.SUSPENDED, user.Status);
        Assert.Null(vendor.ApprovedAt);
        Assert.Null(vendor.ApprovedBy);
        Assert.Equal(DecisionTime, vendor.UpdatedAt);
        Assert.Equal(adminId, vendor.UpdatedBy);
    }

    [Fact]
    public void Suspend_ApprovedVendor_SuspendsUserAndPreservesApprovalProvenance()
    {
        var user = CreateUser(UserStatus.ACTIVE);
        var vendor = CreateVendor(user.Id, ApprovalStatus.APPROVED);
        var originalApprovalTime = DecisionTime.AddDays(-1);
        var originalApprover = Guid.NewGuid();
        vendor.ApprovedAt = originalApprovalTime;
        vendor.ApprovedBy = originalApprover;
        var adminId = Guid.NewGuid();

        vendor.Suspend(user, adminId, DecisionTime);

        Assert.Equal(ApprovalStatus.SUSPENDED, vendor.ApprovalStatus);
        Assert.Equal(UserStatus.SUSPENDED, user.Status);
        Assert.Equal(originalApprovalTime, vendor.ApprovedAt);
        Assert.Equal(originalApprover, vendor.ApprovedBy);
        Assert.Equal(DecisionTime, vendor.UpdatedAt);
        Assert.Equal(adminId, vendor.UpdatedBy);
    }

    [Theory]
    [InlineData(ApprovalStatus.REJECTED, "Approve")]
    [InlineData(ApprovalStatus.APPROVED, "Reject")]
    [InlineData(ApprovalStatus.PENDING, "Suspend")]
    [InlineData(ApprovalStatus.SUSPENDED, "Suspend")]
    public void Decision_InvalidTransition_ThrowsConflict(
        ApprovalStatus status,
        string decision)
    {
        var user = CreateUser(UserStatus.PENDING);
        var vendor = CreateVendor(user.Id, status);
        var adminId = Guid.NewGuid();

        Action action = decision switch
        {
            "Approve" => () => vendor.Approve(user, adminId, DecisionTime),
            "Reject" => () => vendor.Reject(user, adminId, DecisionTime),
            "Suspend" => () => vendor.Suspend(user, adminId, DecisionTime),
            _ => throw new ArgumentOutOfRangeException(nameof(decision))
        };

        var exception = Assert.Throws<InvalidOperationException>(action);

        Assert.Contains("transition", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decision_UserDoesNotOwnVendor_ThrowsConflict()
    {
        var owner = CreateUser(UserStatus.PENDING);
        var otherUser = CreateUser(UserStatus.PENDING);
        var vendor = CreateVendor(owner.Id, ApprovalStatus.PENDING);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            vendor.Approve(otherUser, Guid.NewGuid(), DecisionTime));

        Assert.Contains("user", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static User CreateUser(UserStatus status)
        => new() { Status = status };

    private static VendorProfile CreateVendor(Guid userId, ApprovalStatus status)
        => new() { UserId = userId, ApprovalStatus = status };
}
