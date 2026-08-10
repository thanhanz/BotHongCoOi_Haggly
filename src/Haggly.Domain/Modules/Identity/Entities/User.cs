using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Identity;

public sealed class User : SoftDeletableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.PENDING;
    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public DateTimeOffset? PhoneVerifiedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public BuyerProfile? BuyerProfile { get; set; }
    public VendorProfile? VendorProfile { get; set; }
    public AdminProfile? AdminProfile { get; set; }
    public DelivererProfile? DelivererProfile { get; set; }
}
