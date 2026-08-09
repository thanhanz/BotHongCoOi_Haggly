using Haggly.Api;
using Haggly.Api.Endpoints.Identity;
using Haggly.Api.Endpoints.Identity.Responses;
using Haggly.Api.Responses;
using Haggly.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Haggly.IntegrationTests;

public sealed class IdentityEndpointContractTests
{
    [Fact]
    public void Identity_route_catalog_uses_the_v1_module_prefix()
    {
        Assert.Equal("/api/v1/identity", IdentityRoutes.Prefix);
        Assert.Equal("/api/v1/identity/me", IdentityRoutes.CurrentUserLocation);
    }

    [Fact]
    public async Task Swagger_document_contains_identity_routes_and_bearer_scheme()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "Haggly.Api.Tests",
            ["Jwt:Audience"] = "Haggly.Client.Tests",
            ["Jwt:SigningKey"] = "integration-test-signing-key-that-is-at-least-32-characters",
            ["Jwt:AccessTokenMinutes"] = "15"
        });
        builder.Services.AddTokenServices(builder.Configuration);
        builder.Services.AddApiServices();
        await using var app = builder.Build();

        app.UseSwaggerDocumentation();
        app.MapIdentityEndpoints();

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var document = await client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.True(document.Contains("/api/v1/identity/me", StringComparison.Ordinal), document);
        Assert.Contains("\"Bearer\"", document);
        Assert.Contains("\"scheme\": \"bearer\"", document);
    }

    [Fact]
    public void Identity_routes_are_registered_with_expected_methods()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapIdentityEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToDictionary(endpoint => endpoint.RoutePattern.RawText!);

        Assert.Contains("/api/v1/identity/register/buyer", routes.Keys);
        Assert.Contains("/api/v1/identity/register/vendor", routes.Keys);
        Assert.Contains("/api/v1/identity/login", routes.Keys);
        Assert.Contains("/api/v1/identity/me", routes.Keys);
        Assert.Equal(
            ["POST"],
            routes["/api/v1/identity/register/buyer"].Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
        Assert.Equal(
            ["POST"],
            routes["/api/v1/identity/register/vendor"].Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
        Assert.Equal(
            ["POST"],
            routes["/api/v1/identity/login"].Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
        Assert.Equal(
            ["GET"],
            routes["/api/v1/identity/me"].Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public void Current_user_route_requires_authorization()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapIdentityEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText == "/api/v1/identity/me");

        Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData);
    }

    [Theory]
    [InlineData("/api/v1/identity/register/buyer", typeof(ApiResponse<RegistrationResponse>))]
    [InlineData("/api/v1/identity/register/vendor", typeof(ApiResponse<RegistrationResponse>))]
    [InlineData("/api/v1/identity/login", typeof(ApiResponse<LoginResponse>))]
    [InlineData("/api/v1/identity/me", typeof(ApiResponse<CurrentUserResponse>))]
    public void Successful_identity_responses_use_the_api_response_envelope(
        string route,
        Type responseType)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapIdentityEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == route);

        Assert.Contains(
            endpoint.Metadata.OfType<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode is 200 or 201 && metadata.Type == responseType);
    }

    [Theory]
    [InlineData("/api/v1/identity/register/buyer", 400)]
    [InlineData("/api/v1/identity/register/buyer", 409)]
    [InlineData("/api/v1/identity/register/vendor", 400)]
    [InlineData("/api/v1/identity/register/vendor", 409)]
    [InlineData("/api/v1/identity/login", 400)]
    [InlineData("/api/v1/identity/login", 401)]
    [InlineData("/api/v1/identity/me", 401)]
    public void Identity_failures_are_documented_as_problem_details(
        string route,
        int statusCode)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapIdentityEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == route);

        Assert.Contains(
            endpoint.Metadata.OfType<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == statusCode && metadata.Type == typeof(ProblemDetails));
    }
}
