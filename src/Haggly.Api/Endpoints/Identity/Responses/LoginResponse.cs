using Haggly.Application.Modules.Identity.Login.Dtos;

namespace Haggly.Api.Endpoints.Identity.Responses;

public sealed record LoginResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    IReadOnlyCollection<string> Roles)
{
    public static LoginResponse From(LoginResult result)
        => new(
            result.UserId,
            result.Email,
            result.AccessToken,
            result.TokenType,
            result.ExpiresAt,
            result.Roles.Select(role => role.ToString()).ToArray());
}
