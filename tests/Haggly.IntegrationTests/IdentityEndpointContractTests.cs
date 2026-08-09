using Haggly.Api.Endpoints.Identity;
using Haggly.Api.Endpoints.Identity.Responses;
using Haggly.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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
}
