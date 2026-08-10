using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Modules.Identity.Login.Dtos;

public sealed record LoginResult(
    Guid UserId,
    string Email,
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    IReadOnlyCollection<RoleCode> Roles);
