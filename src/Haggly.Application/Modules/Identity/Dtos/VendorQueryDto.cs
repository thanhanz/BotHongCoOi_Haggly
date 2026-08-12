using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Modules.Identity.Dtos;

public sealed record VendorQueryDto(
    Guid UserId,
    string Email,
    string PhoneNumber,
    string FullName,
    string BusinessName,
    string? BusinessRegistrationNo,
    string? TaxCode,
    UserStatus UserStatus,
    ApprovalStatus ApprovalStatus,
    DateTimeOffset? ApprovedAt,
    Guid? ApprovedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy)
{
    public static VendorQueryDto From(User user, VendorProfile vendor)
        => new(
            user.Id,
            user.Email,
            user.PhoneNumber,
            user.FullName,
            vendor.BusinessName,
            vendor.BusinessRegistrationNo,
            vendor.TaxCode,
            user.Status,
            vendor.ApprovalStatus,
            vendor.ApprovedAt,
            vendor.ApprovedBy,
            vendor.CreatedAt,
            vendor.UpdatedAt,
            vendor.UpdatedBy);
}
