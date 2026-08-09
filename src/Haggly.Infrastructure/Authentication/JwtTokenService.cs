using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Login.Dtos;
using Haggly.Domain.Modules.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Haggly.Infrastructure.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IIdentityTokenService
{
    public IssuedAccessToken CreateAccessToken(
        User user,
        IReadOnlyCollection<RoleCode> roles)
    {
        var settings = options.Value;
        if (!settings.IsValid())
            throw new InvalidOperationException("JWT configuration is invalid.");

        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddMinutes(settings.AccessTokenMinutes);
        var userId = user.Id.ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
