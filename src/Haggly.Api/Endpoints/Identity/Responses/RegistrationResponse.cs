using Haggly.Application.Modules.Identity.Registration.Dtos;

namespace Haggly.Api.Endpoints.Identity.Responses;

public sealed record RegistrationResponse(
    Guid UserId,
    string Email,
    string Status,
    string Role)
{
    public static RegistrationResponse From(RegistrationResult result)
        => new(result.UserId, result.Email, result.Status.ToString(), result.RoleCode.ToString());
}
