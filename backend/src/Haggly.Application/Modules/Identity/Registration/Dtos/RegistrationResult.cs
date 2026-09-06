using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Modules.Identity.Registration.Dtos;

public sealed record RegistrationResult(
    Guid UserId,
    string Email,
    UserStatus Status,
    RoleCode RoleCode);
