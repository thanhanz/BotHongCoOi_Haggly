using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace Haggly.UnitTests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_contains_identity_and_role_claims()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "Haggly.Api",
            Audience = "Haggly.Client",
            SigningKey = "development-only-signing-key-change-before-production-123456",
            AccessTokenMinutes = 15
        }));
        var user = new User
        {
            Email = "buyer@example.com"
        };

        var result = service.CreateAccessToken(user, [RoleCode.BUYER, RoleCode.VENDOR]);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);

        Assert.Equal("Haggly.Api", token.Issuer);
        Assert.Contains("Haggly.Client", token.Audiences);
        Assert.Contains(token.Claims, claim => claim.Type == "sub" && claim.Value == user.Id.ToString());
        Assert.Contains(token.Claims, claim => claim.Type == "email" && claim.Value == user.Email);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Iat);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "BUYER");
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "VENDOR");
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateAccessToken_rejects_invalid_jwt_configuration()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions()));

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateAccessToken(new User(), [RoleCode.BUYER]));
    }
}
