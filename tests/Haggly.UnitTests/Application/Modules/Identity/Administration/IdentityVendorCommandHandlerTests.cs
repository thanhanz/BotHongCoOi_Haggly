using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Administration;
using Haggly.Application.Modules.Identity.Administration.Commands;
using Haggly.Domain.Modules.Identity;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Identity.Administration;

public sealed class IdentityVendorCommandHandlerTests
{
    private static readonly DateTimeOffset DecisionTime =
        new(2026, 8, 12, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApproveVendor_PendingVendor_ActivatesAndReturnsUpdatedDto()
    {
        var aggregate = CreateAggregate(ApprovalStatus.PENDING, UserStatus.PENDING);
        var repository = new RecordingVendorAdminCommandRepository(aggregate);
        var handler = new ApproveVendorHandler(repository, new FixedTimeProvider(DecisionTime));
        var adminId = Guid.NewGuid();

        var result = await handler.Handle(
            new ApproveVendorCommand(aggregate.User.Id, adminId), CancellationToken.None);

        Assert.Equal(ApprovalStatus.APPROVED, result.ApprovalStatus);
        Assert.Equal(UserStatus.ACTIVE, result.UserStatus);
        Assert.Equal(adminId, result.ApprovedBy);
        Assert.Equal(DecisionTime, result.ApprovedAt);
        Assert.True(repository.WasSaved);
    }

    [Fact]
    public async Task RejectVendor_PendingVendor_SuspendsAndReturnsUpdatedDto()
    {
        var aggregate = CreateAggregate(ApprovalStatus.PENDING, UserStatus.PENDING);
        var repository = new RecordingVendorAdminCommandRepository(aggregate);
        var handler = new RejectVendorHandler(repository, new FixedTimeProvider(DecisionTime));

        var result = await handler.Handle(
            new RejectVendorCommand(aggregate.User.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ApprovalStatus.REJECTED, result.ApprovalStatus);
        Assert.Equal(UserStatus.SUSPENDED, result.UserStatus);
        Assert.True(repository.WasSaved);
    }

    [Fact]
    public async Task SuspendVendor_ApprovedVendor_SuspendsAndReturnsUpdatedDto()
    {
        var aggregate = CreateAggregate(ApprovalStatus.APPROVED, UserStatus.ACTIVE);
        var repository = new RecordingVendorAdminCommandRepository(aggregate);
        var handler = new SuspendVendorHandler(repository, new FixedTimeProvider(DecisionTime));

        var result = await handler.Handle(
            new SuspendVendorCommand(aggregate.User.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ApprovalStatus.SUSPENDED, result.ApprovalStatus);
        Assert.Equal(UserStatus.SUSPENDED, result.UserStatus);
        Assert.True(repository.WasSaved);
    }

    [Fact]
    public async Task ApproveVendor_UnknownVendor_ThrowsNotFoundWithoutSaving()
    {
        var repository = new RecordingVendorAdminCommandRepository();
        var handler = new ApproveVendorHandler(repository, new FixedTimeProvider(DecisionTime));

        await Assert.ThrowsAsync<VendorNotFoundException>(() =>
            handler.Handle(new ApproveVendorCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        Assert.False(repository.WasSaved);
    }

    [Fact]
    public async Task RejectVendor_ApprovedVendor_ThrowsConflictWithoutSaving()
    {
        var aggregate = CreateAggregate(ApprovalStatus.APPROVED, UserStatus.ACTIVE);
        var repository = new RecordingVendorAdminCommandRepository(aggregate);
        var handler = new RejectVendorHandler(repository, new FixedTimeProvider(DecisionTime));

        await Assert.ThrowsAsync<VendorTransitionConflictException>(() =>
            handler.Handle(new RejectVendorCommand(aggregate.User.Id, Guid.NewGuid()), CancellationToken.None));

        Assert.False(repository.WasSaved);
    }

    private static VendorAdminAggregate CreateAggregate(
        ApprovalStatus approvalStatus,
        UserStatus userStatus)
    {
        var user = new User { Status = userStatus, Email = "vendor@example.com", FullName = "Vendor" };
        return new VendorAdminAggregate(
            user,
            new VendorProfile { UserId = user.Id, BusinessName = "Vendor Stall", ApprovalStatus = approvalStatus });
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingVendorAdminCommandRepository(VendorAdminAggregate? aggregate = null)
        : IVendorAdminCommandRepository
    {
        private readonly VendorAdminAggregate? aggregate = aggregate;
        public bool WasSaved { get; private set; }

        public Task<VendorAdminAggregate?> FindByIdAsync(Guid vendorId, CancellationToken cancellationToken)
            => Task.FromResult(aggregate?.User.Id == vendorId ? aggregate : null);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            WasSaved = true;
            return Task.CompletedTask;
        }
    }
}
