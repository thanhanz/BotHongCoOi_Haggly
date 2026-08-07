using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Identity;

public sealed class User : SoftDeletableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public DateTimeOffset? PhoneVerifiedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public BuyerProfile? BuyerProfile { get; set; }
    public VendorProfile? VendorProfile { get; set; }
    public AdminProfile? AdminProfile { get; set; }
    public DelivererProfile? DelivererProfile { get; set; }
}

public sealed class Role : SoftDeletableEntity
{
    public RoleCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public sealed class UserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? AssignedBy { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public User? User { get; set; }
    public Role? Role { get; set; }
}

public sealed class BuyerProfile : AuditableRecord
{
    public Guid UserId { get; set; }
    public string? DefaultPickupNote { get; set; }
    public Guid? DefaultPaymentMethodId { get; set; }
}

public sealed class VendorProfile : AuditableRecord
{
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessRegistrationNo { get; set; }
    public string? TaxCode { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
}

public sealed class AdminProfile : AuditableRecord
{
    public Guid UserId { get; set; }
    public string? EmployeeCode { get; set; }
    public AdminScope AdminScope { get; set; }
}

public sealed class DelivererProfile : AuditableRecord
{
    public Guid UserId { get; set; }
    public VehicleType VehicleType { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
}
