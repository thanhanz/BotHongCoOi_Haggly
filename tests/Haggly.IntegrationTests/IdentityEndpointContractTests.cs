using Haggly.Api.Endpoints.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests;

public sealed class IdentityEndpointContractTests
{
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

        Assert.Contains("/api/auth/register/buyer", routes.Keys);
        Assert.Contains("/api/auth/register/vendor", routes.Keys);
        Assert.Contains("/api/auth/login", routes.Keys);
        Assert.Contains("/api/auth/me", routes.Keys);
        Assert.Equal(
            ["POST"],
            routes["/api/auth/register/buyer"].Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
        Assert.Equal(
            ["POST"],
            routes["/api/auth/register/vendor"].Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
        Assert.Equal(
            ["POST"],
            routes["/api/auth/login"].Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
        Assert.Equal(
            ["GET"],
            routes["/api/auth/me"].Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
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
            .Single(route => route.RoutePattern.RawText == "/api/auth/me");

        Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData);
    }
}
