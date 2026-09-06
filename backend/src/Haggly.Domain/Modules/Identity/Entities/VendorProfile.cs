using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Identity;

public sealed class VendorProfile : AuditableRecord
{
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessRegistrationNo { get; set; }
    public string? TaxCode { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.PENDING;
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }

    public void Approve(User user, Guid approvedBy, DateTimeOffset decidedAt)
    {
        EnsureOwnsUser(user);
        EnsureDecisionContext(approvedBy, decidedAt);

        if (ApprovalStatus is not (ApprovalStatus.PENDING or ApprovalStatus.SUSPENDED))
            throw InvalidTransition("approve");

        ApprovalStatus = ApprovalStatus.APPROVED;
        user.Status = UserStatus.ACTIVE;
        ApprovedAt = decidedAt;
        ApprovedBy = approvedBy;
        MarkUpdated(approvedBy, decidedAt);
    }

    public void Reject(User user, Guid rejectedBy, DateTimeOffset decidedAt)
    {
        EnsureOwnsUser(user);
        EnsureDecisionContext(rejectedBy, decidedAt);

        if (ApprovalStatus != ApprovalStatus.PENDING)
            throw InvalidTransition("reject");

        ApprovalStatus = ApprovalStatus.REJECTED;
        user.Status = UserStatus.SUSPENDED;
        ApprovedAt = null;
        ApprovedBy = null;
        MarkUpdated(rejectedBy, decidedAt);
    }

    public void Suspend(User user, Guid suspendedBy, DateTimeOffset decidedAt)
    {
        EnsureOwnsUser(user);
        EnsureDecisionContext(suspendedBy, decidedAt);

        if (ApprovalStatus != ApprovalStatus.APPROVED)
            throw InvalidTransition("suspend");

        ApprovalStatus = ApprovalStatus.SUSPENDED;
        user.Status = UserStatus.SUSPENDED;
        MarkUpdated(suspendedBy, decidedAt);
    }

    private void EnsureOwnsUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id != UserId)
            throw new InvalidOperationException("The vendor profile does not belong to the supplied user.");
    }

    private static void EnsureDecisionContext(Guid actorId, DateTimeOffset decidedAt)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("The decision actor is required.", nameof(actorId));

        if (decidedAt == default)
            throw new ArgumentException("The decision timestamp is required.", nameof(decidedAt));
    }

    private static InvalidOperationException InvalidTransition(string decision)
        => new($"The vendor cannot transition to the requested state through {decision}.");

    private void MarkUpdated(Guid actorId, DateTimeOffset updatedAt)
    {
        UpdatedBy = actorId;
        UpdatedAt = updatedAt;
    }
}
