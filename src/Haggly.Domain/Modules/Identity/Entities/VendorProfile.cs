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
}
