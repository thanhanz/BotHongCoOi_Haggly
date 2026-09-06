using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Haggly.Application.Abstractions.Identity;

namespace Haggly.Api.Authentication;

public sealed class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public bool IsAuthenticated
        => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var subject = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!IsAuthenticated || !Guid.TryParse(subject, out var userId))
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user identifier is missing or invalid.");
            }

            return userId;
        }
    }
}
