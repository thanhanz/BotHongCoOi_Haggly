using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using Haggly.Api;
using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Identity;
using Haggly.Api.Middleware;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Haggly.IntegrationTests.Api.Authorization;

public sealed class AuthenticationPipelineTests
{
    [Fact]
    public void Token_services_configure_strict_bearer_validation()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authentication.DefaultAuthenticateScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authentication.DefaultChallengeScheme);
        Assert.True(bearer.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(bearer.TokenValidationParameters.ValidateIssuer);
        Assert.True(bearer.TokenValidationParameters.ValidateAudience);
        Assert.True(bearer.TokenValidationParameters.ValidateLifetime);
        Assert.Equal("Haggly.Api.Tests", bearer.TokenValidationParameters.ValidIssuer);
        Assert.Equal("Haggly.Client.Tests", bearer.TokenValidationParameters.ValidAudience);
        Assert.Equal(TimeSpan.Zero, bearer.TokenValidationParameters.ClockSkew);
        Assert.Equal("roles", bearer.TokenValidationParameters.RoleClaimType);
    }

    [Fact]
    public async Task Issued_token_is_authenticated_and_authorized_by_role()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var tokenService = provider.GetRequiredService<Haggly.Application.Abstractions.Identity.IIdentityTokenService>();
        var token = tokenService.CreateAccessToken(
            new User { Email = "buyer@example.com" },
            [RoleCode.BUYER]);
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Headers.Authorization = $"Bearer {token.Value}";

        var result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        var authorization = await provider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(result.Principal!, resource: null, IdentityPolicies.BuyerOnly);

        Assert.True(result.Succeeded);
        Assert.True(authorization.Succeeded);
        Assert.True(result.Principal!.IsInRole(RoleCode.BUYER.ToString()));
    }

    [Fact]
    public async Task Buyer_token_is_forbidden_by_vendor_policy()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var tokenService = provider.GetRequiredService<Haggly.Application.Abstractions.Identity.IIdentityTokenService>();
        var token = tokenService.CreateAccessToken(
            new User { Email = "buyer@example.com" },
            [RoleCode.BUYER]);
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Headers.Authorization = $"Bearer {token.Value}";
        var authentication = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        var authorization = await provider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(authentication.Principal!, resource: null, IdentityPolicies.VendorOnly);

        Assert.False(authorization.Succeeded);
    }

    [Theory]
    [InlineData(false, StatusCodes.Status401Unauthorized, "Authentication required")]
    [InlineData(true, StatusCodes.Status403Forbidden, "Access forbidden")]
    public async Task Authentication_failures_use_problem_details(
        bool forbid,
        int expectedStatus,
        string expectedTitle)
    {
        using var provider = CreateServices().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Path = "/api/v1/identity/me";
        context.Response.Body = new MemoryStream();

        if (forbid)
            await context.ForbidAsync();
        else
            await context.ChallengeAsync();

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        Assert.Contains(expectedTitle, body);
        if (!forbid)
            Assert.Equal("Bearer", context.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public async Task Http_pipeline_enforces_token_lifetime_and_role_policies()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var configuration = CreateConfiguration();
        builder.Services.AddTokenServices(configuration);
        builder.Services.AddApiServices();
        await using var app = builder.Build();

        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapIdentityEndpoints();
        app.MapGet("/api/v1/test/vendor", () => Results.Ok())
            .RequireAuthorization(IdentityPolicies.VendorOnly);

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };

        var missing = await client.GetAsync("/api/v1/identity/me");
        var malformed = await SendWithTokenAsync(client, "/api/v1/identity/me", "not-a-jwt");
        var expired = await SendWithTokenAsync(client, "/api/v1/identity/me", CreateExpiredToken());

        var user = new User { Email = "buyer@example.com" };
        var token = app.Services
            .GetRequiredService<Haggly.Application.Abstractions.Identity.IIdentityTokenService>()
            .CreateAccessToken(user, [RoleCode.BUYER]);
        var currentUser = await SendWithTokenAsync(client, "/api/v1/identity/me", token.Value);
        var forbidden = await SendWithTokenAsync(client, "/api/v1/test/vendor", token.Value);

        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, malformed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        Assert.Equal(HttpStatusCode.OK, currentUser.StatusCode);
        Assert.Contains(user.Email, await currentUser.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Contains("Access forbidden", await forbidden.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Application_exception_is_written_as_problem_details()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        await using var disposableServices = services;
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = "/api/v1/identity/register/buyer";
        context.Response.Body = new MemoryStream();

        var handler = new ApiExceptionHandler(
            services.GetRequiredService<IProblemDetailsService>(),
            NullLogger<ApiExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new RegistrationConflictException("Email already exists."),
            CancellationToken.None);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Contains("Registration conflict", body);
        Assert.Contains("Email already exists.", body);
    }

    [Fact]
    public async Task Authorization_policies_use_the_seeded_role_codes()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var buyer = await policies.GetPolicyAsync(IdentityPolicies.BuyerOnly);
        var vendor = await policies.GetPolicyAsync(IdentityPolicies.VendorOnly);
        var admin = await policies.GetPolicyAsync(IdentityPolicies.AdminOnly);

        AssertPolicyRoles(buyer!, RoleCode.BUYER);
        AssertPolicyRoles(vendor!, RoleCode.VENDOR);
        AssertPolicyRoles(admin!, RoleCode.MARKET_ADMIN, RoleCode.PLATFORM_ADMIN);
    }

    private static IServiceCollection CreateServices()
    {
        return new ServiceCollection()
            .AddLogging()
            .AddTokenServices(CreateConfiguration())
            .AddApiServices();
    }

    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "Haggly.Api.Tests",
                ["Jwt:Audience"] = "Haggly.Client.Tests",
                ["Jwt:SigningKey"] = "integration-test-signing-key-that-is-at-least-32-characters",
                ["Jwt:AccessTokenMinutes"] = "15"
            })
            .Build();

    private static async Task<HttpResponseMessage> SendWithTokenAsync(
        HttpClient client,
        string path,
        string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static string CreateExpiredToken()
    {
        var now = DateTime.UtcNow;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                "integration-test-signing-key-that-is-at-least-32-characters")),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "Haggly.Api.Tests",
            audience: "Haggly.Client.Tests",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())],
            notBefore: now.AddMinutes(-2),
            expires: now.AddMinutes(-1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AssertPolicyRoles(
        AuthorizationPolicy policy,
        params RoleCode[] expectedRoles)
    {
        var requirement = Assert.Single(policy.Requirements.OfType<RolesAuthorizationRequirement>());
        Assert.Equal(
            expectedRoles.Select(role => role.ToString()).Order(),
            requirement.AllowedRoles.Order());
    }
}
