using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Modules.Identity.Dtos;

public sealed record VendorAdminDto(
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
    Guid? UpdatedBy);
