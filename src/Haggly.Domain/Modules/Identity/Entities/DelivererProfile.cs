using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Identity;

public sealed class DelivererProfile : AuditableRecord
{
    public Guid UserId { get; set; }
    public VehicleType VehicleType { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.PENDING;
}
